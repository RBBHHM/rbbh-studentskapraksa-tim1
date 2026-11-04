"""Scan reachable Git history; print locations/categories, never matched values.

This is a publication guard for literal credentials, not credential revocation or
a certification that arbitrary encrypted/binary content contains no secret.
"""
import argparse
import hashlib
import json
import re
import subprocess
import sys

LITERAL = re.compile(
    r'''["']?([A-Za-z0-9_.:-]*(?:client[_-]?secret|password|api[_-]?key|access[_-]?token))["']?\s*(?:\]\s*)?[:=]\s*["']([^"'\r\n]*)["']''',
    re.IGNORECASE,
)
CONNECTION = re.compile(r'''[;"']\s*(?:password|pwd)\s*=\s*([^;"'\r\n]*)''', re.IGNORECASE)
CONFIG_SCALAR = re.compile(r"^[ \t]*(?:export[ \t]+)?([A-Za-z0-9_:.-]*(?:secret|password|token|api[_-]?key))[ \t]*[:=][ \t]*(\S[^\r\n#]*)", re.IGNORECASE | re.MULTILINE)
TOKEN = re.compile(r"\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|AKIA[A-Z0-9]{16})\b")
PRIVATE_KEY = re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH |ENCRYPTED )?PRIVATE KEY-----(?:\s|\\[rn])+[A-Za-z0-9+/=\\\r\n ]{32,}-----END (?:RSA |EC |OPENSSH |ENCRYPTED )?PRIVATE KEY-----")
FIXTURES = {"fixture", "fixture-only", "test", "test-only", "test-password", "sql-password", "password", "pass", "secret", "ignored"}
# Pure BuildConnectionString assertion; this digest exception is scoped to this reviewed unit-test file.
REVIEWED_FIXTURE_DIGESTS = {"tests/Backend.UnitTests/Helpers/ConnectionHelperTests.cs": {"89e01536ac207279409d4de1e5253e01f4a1769e696db0d6062ca9b8f56767c8"}}


def placeholder(value):
    return not value.strip() or value.startswith(("${", "__", "<")) or bool(re.fullmatch(r"\$(?:env:)?[A-Za-z_][A-Za-z0-9_]*", value)) or value.upper().startswith(("YOUR_", "REPLACE_")) or value.upper() in {"REDACTED", "CHANGEME", "CHANGE_ME"}


def test_literal(value):
    return value in FIXTURES or value.startswith(("synthetic-regression-", "regression-")) or bool(re.search(r"(?:^|[-_\s])(?:fixture|test|unit|dummy|smoke|unused|configured|local|placeholder)(?:[-_\s]|$)|(?:^|[-_\s])not-a[-_]", value, re.IGNORECASE))


def reviewed_fixture(path, value):
    return hashlib.sha256(value.encode()).hexdigest() in REVIEWED_FIXTURE_DIGESTS.get(path.replace("\\", "/"), set())


def findings(path, data):
    """Return only rule/key/line. Never return credential material."""
    text = data.decode("utf-8-sig", errors="replace")
    found = []
    fixture = path.replace("\\", "/").startswith(("tests/", "scripts/security/test_", "scripts/testing/", "RelatedPartiesRegister/UnitTests/", "RelatedPartiesRegister/IntegrationTests/"))
    for match in LITERAL.finditer(text):
        value = match.group(2)
        # A quoted empty CLI argument followed by a list separator is not a value.
        if match.group(1).startswith("--") and value.strip() == ",":
            continue
        if placeholder(value) or fixture and (test_literal(value) or reviewed_fixture(path, value)):
            continue
        found.append({"rule": "credential-literal", "key": match.group(1), "line": text.count("\n", 0, match.start()) + 1})
    for match in CONNECTION.finditer(text):
        value = match.group(1)
        if placeholder(value) or fixture and (test_literal(value) or reviewed_fixture(path, value)):
            continue
        found.append({"rule": "connection-password", "line": text.count("\n", 0, match.start()) + 1})
    if path.lower().endswith((".env", ".env.example", ".yml", ".yaml", ".properties")):
        for match in CONFIG_SCALAR.finditer(text):
            if match.group(1).startswith("--"):
                continue  # Shell flags are checked by LITERAL, not YAML scalar syntax.
            value = match.group(2).strip().strip("\"'")
            if value.startswith("#") or placeholder(value) or fixture and test_literal(value):
                continue
            found.append({"rule": "config-credential", "key": match.group(1), "line": text.count("\n", 0, match.start()) + 1})
    for rule, pattern in (("provider-token", TOKEN), ("private-key", PRIVATE_KEY)):
        for match in pattern.finditer(text):
            found.append({"rule": rule, "line": text.count("\n", 0, match.start()) + 1})
    return found


def git(*arguments, data=None):
    return subprocess.check_output(["git", *arguments], input=data, stderr=subprocess.PIPE)


def scan(ref):
    commit = git("rev-parse", "--verify", ref + "^{commit}").decode().strip()
    objects = {}
    for line in git("rev-list", "--objects", commit).decode().splitlines():
        oid, _, name = line.partition(" ")
        objects[oid] = name
    metadata = git("cat-file", "--batch-check=%(objectname) %(objecttype) %(objectsize)", data=("\n".join(objects) + "\n").encode()).decode().splitlines()
    selected = []
    for row in metadata:
        oid, kind, size = row.split()
        name = objects[oid]
        if kind in {"commit", "blob"}:
            selected.append((oid, kind, name))
    raw = git("cat-file", "--batch", data=("\n".join(row[0] for row in selected) + "\n").encode())
    position = 0
    result = []
    for oid, kind, name in selected:
        end = raw.index(b"\n", position)
        size = int(raw[position:end].split()[-1])
        data = raw[end + 1:end + 1 + size]
        position = end + size + 2
        location = name if kind == "blob" else "(commit message)"
        for finding in findings(location, data):
            result.append({"object": oid, "path": location, **finding})
    return {"ref": commit, "objects_checked": len(selected), "finding_count": len(result), "findings": result}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ref", default="HEAD", help="Commit/ref whose complete reachable history must be checked")
    args = parser.parse_args()
    try:
        result = scan(args.ref)
        print(json.dumps(result, indent=2))
        return 1 if result["finding_count"] else 0
    except (subprocess.CalledProcessError, ValueError, IndexError):
        print("Secret scan could not inspect the requested Git history; failing closed.", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
