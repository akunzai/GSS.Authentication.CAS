# ASP.NET Core React.js Sample

## Requirement

- [Bun](https://bun.com) (recommended via [mise](https://mise.jdx.dev): `mise install`)

## Getting Started

```sh
# install npm packages
bun install --frozen-lockfile

# watch and serve a dev server at https://localhost:3000/
bun run start

# build the project in production mode. The build artifacts will be stored in the `dist/` directory
bun run build

# lint (oxlint) and check formatting (bun fmt)
bun run lint

# write formatting changes (bun fmt)
bun run fmt
```

`bun check` checks types before Vite builds the client. Bun also runs the Vite dev server and HTTPS setup scripts. Formatting options live in `bunfig.toml`; Oxlint keeps its `.oxlintrc.json` configuration.
