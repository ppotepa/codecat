# Adding a built-in plugin

1. Add one definition under `BuiltIns/`, using the existing `ExtensionPlugin` for extension, suffix, and exact-name rules.
2. Register it once in `BuiltInPluginCatalog`. Set an explicit rule priority for intentional collisions; equal priority and specificity use catalog order as the tie breaker, and the catalog rejects unresolved conflicts.
3. Add a test for a matching filename, a non-match, and any collision or multi-part suffix.
4. Keep global directory exclusions in Core scan policy; plugins describe files, not global traversal behavior.

Plugin code is compiled into the Native AOT executable. Do not add reflection-based discovery or runtime assembly loading.
