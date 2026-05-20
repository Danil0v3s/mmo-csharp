# JSON Schemas

This folder holds the JSON Schema (draft-2020-12) files used by every
JSON config in the repo. Every editor that supports JSON Schema
(VS Code, Visual Studio 2022, JetBrains Rider, Sublime LSP) gets:

- **Autocomplete** for top-level + nested property names.
- **Hover docs** showing each property's purpose (sourced from rAthena's
  own inline `.conf` comments).
- **Type validation** (number / boolean / string / object).
- **Default values** displayed alongside each entry.

## How a JSON file finds its schema

Each JSON config carries a `$schema` property pointing at the schema
file path. Examples:

```jsonc
// Map.Server/appsettings.json
{
  "$schema": "../schemas/appsettings.shared.schema.json",
  ...
}

// Map.Server/config/battle/battle.json
{
  "$schema": "./battle.schema.json",
  "enable_baseatk": 9,
  ...
}
```

## Schemas in this folder

| Schema | Used by | Source |
|---|---|---|
| `appsettings.shared.schema.json` | Every server's `appsettings.json` | Hand-written |

## Schemas generated under `Map.Server/config/`

The rAthena `.conf` files are converted to JSON via
`Tools.RathenaImporter --conf-only`. Each generated JSON file has a
sibling `.schema.json` whose `description` fields come from the
rAthena `.conf` comments.

| Generated file | Source |
|---|---|
| `Map.Server/config/battle/<name>.json` + `.schema.json` | `rathena/conf/battle/<name>.conf` |
| `Map.Server/config/channels.json` + `.schema.json` | `rathena/conf/channels.conf` |

Re-run `dotnet run --project Tools.RathenaImporter --conf-only` to
refresh the conf-derived JSONs + schemas from upstream rAthena.

## Adding a new schema

For a new hand-written config (a new `appsettings.<env>.json`,
a runtime config for a new server tier, etc.):

1. Drop the schema file in this folder.
2. Add a `$schema` reference to the JSON file pointing at the schema.
3. Document each property with `description` + sensible `type` /
   `default` / `minimum` / `maximum` constraints.

For a converted rAthena config — add it to the `confTargets[]` array
in `Tools.RathenaImporter/Program.cs` and re-run the tool.
