# MIGRATION_REFERENCE — Related Parties React frontend

This directory is retained as migration/reference and rollback source, not the
target frontend architecture. The native implementation is in `../Web.Blazor`.
Do not add new business features here as part of the Blazor migration.

**This marker does not mean production cutover has happened.** Existing scripts
still build this tree; deployed IIS/runtime-config consumers and OCP routing have
not been independently verified. Do not delete or relocate this directory until
those consumers are retired through an approved deployment/rollback procedure.

See `docs/BLAZOR-MIGRATION-RETIREMENT-AND-DEPLOYMENT.md` and
`docs/REACT-TO-BLAZOR-FINAL-HANDOFF.md` at repository root. No React implementation
or existing deployment is changed by this marker.
