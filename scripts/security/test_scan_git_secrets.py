from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

import scan_git_secrets as scanner


class PublicationSecretTests(unittest.TestCase):
    def test_nonempty_json_or_dotnet_key_is_reported_without_value(self):
        secret = "regression-private-sentinel-123"
        for body in ('{"ClientSecret":"' + secret + '"}', '["KeycloakSettings:ClientSecret"] = "' + secret + '"'):
            found = scanner.findings("Frontend/appsettings.json", body.encode())
            self.assertTrue(found)
            self.assertNotIn(secret, str(found))

    def test_empty_and_external_configuration_placeholders_are_allowed(self):
        for value in ("", "${APP_SECRET}", "__EXTERNAL_SECRET__", "<user-secrets>"):
            self.assertFalse(scanner.findings("appsettings.json", ('{"ClientSecret":"' + value + '"}').encode()))

    def test_fixture_exception_is_restricted_to_tests(self):
        body = b'{"ClientSecret":"fixture-only"}'
        self.assertFalse(scanner.findings("tests/Backend.IntegrationTests/Fixture.cs", body))
        self.assertTrue(scanner.findings("Frontend/appsettings.json", body))
        opaque = "opaque-" + "production-credential"
        self.assertTrue(scanner.findings("tests/Fixture.cs", b'{"ClientSecret":"' + opaque.encode() + b'"}'))
        self.assertFalse(scanner.findings("tests/OptionsTests.cs", b'{"ClientSecret":"configured-placeholder"}'))
        self.assertTrue(scanner.findings("appsettings.json", b'{"ClientSecret":"configured-placeholder"}'))

    def test_environment_references_are_not_literal_credentials(self):
        self.assertFalse(scanner.findings("workflow.yml", b'--docker-password "$APP_PASSWORD"'))

    def test_reviewed_fixture_digest_is_restricted_to_its_exact_test_path(self):
        value = "opaque-" + "credential"
        raw = b'{"ClientSecret":"' + value.encode() + b'"}'
        with patch.dict(scanner.REVIEWED_FIXTURE_DIGESTS, {"tests/Reviewed.cs": {scanner.hashlib.sha256(value.encode()).hexdigest()}}):
            self.assertFalse(scanner.findings("tests/Reviewed.cs", raw))
            self.assertTrue(scanner.findings("tests/Other.cs", raw))

    def test_empty_cli_argument_is_not_a_punctuation_password(self):
        self.assertFalse(scanner.findings("scripts/testing/Smoke.ps1", b"'--Authentication:ClientSecret=','--Apis:BaseUrl='"))

    def test_builder_assignment_and_enum_are_not_connection_strings(self):
        self.assertFalse(scanner.findings("Helper.cs", b'Pass' + b'word = GetRequiredValue(configuration, "Database:Password")'))
        self.assertFalse(scanner.findings("PublicAPI.txt", b'Pass' + b'word = 1,'))

    def test_connection_password_is_reported(self):
        self.assertTrue(scanner.findings("settings.config", b'Server=local;Password=synthetic-regression-value;Encrypt=true'))
        self.assertTrue(scanner.findings("settings.config", b'"Server=local;Password=synthetic-regression-value"'))

    def test_unquoted_environment_and_yaml_passwords_are_reported(self):
        self.assertTrue(scanner.findings(".env", b'KEYCLOAK_CLIENT_SECRET=opaque-production-credential'))
        self.assertTrue(scanner.findings("config.yml", b'Database__Password: opaque-production-credential'))
        self.assertFalse(scanner.findings("config.yml", b'KEYCLOAK_CLIENT_SECRET: ${APP_SECRET}'))

    def test_empty_environment_value_does_not_consume_the_next_line(self):
        self.assertFalse(scanner.findings(".env.example", b'CLIENT_SECRET=\nDATABASE_HOST=example.test\n'))
        self.assertFalse(scanner.findings(".env.example", b'CLIENT_SECRET= # supplied externally\n'))

    def test_private_key_and_provider_token_are_reported(self):
        self.assertTrue(scanner.findings("private.key", b"-----BEGIN PRIVATE KEY-----\n" + b"A" * 64 + b"\n-----END PRIVATE KEY-----"))
        self.assertTrue(scanner.findings("config.json", ("ghp_" + "A" * 36).encode()))

    def test_removed_secret_in_an_ancestor_still_fails_scan(self):
        sentinel = "regression-history-sentinel-456"
        with tempfile.TemporaryDirectory(prefix="rbbh-secret-scan-") as directory:
            def git(*args):
                return subprocess.check_output(["git", "-C", directory, *args], stderr=subprocess.DEVNULL)
            git("init")
            git("config", "user.name", "Security regression")
            git("config", "user.email", "security@example.test")
            path = Path(directory) / "appsettings.json"
            path.write_text('{"ClientSecret":"' + sentinel + '"}', encoding="utf-8")
            git("add", ".")
            git("commit", "-m", "Synthetic fixture ancestor")
            path.write_text('{"ClientSecret":""}', encoding="utf-8")
            git("add", ".")
            git("commit", "-m", "Synthetic fixture removal")
            command = subprocess.run([sys.executable, str(Path(scanner.__file__).resolve()), "--ref", "HEAD"],
                cwd=directory, capture_output=True, text=True)
            self.assertEqual(1, command.returncode)
            self.assertNotIn(sentinel, command.stdout + command.stderr)
            self.assertIn('"finding_count": 1', command.stdout)


if __name__ == "__main__":
    unittest.main()
