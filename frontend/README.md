# Nitrotype Tracker

Tracker for NitroType teams statistics.

## History

- Install Prettier & ESLint to VS Code / Rider (already installed in Rider), turn on Automatic prettier in Rider + Run on save, Automatic ESLint + Fix on save
- `ng new overlab --prefix olab --directory frontend --skip-tests --experimental-zoneless --style scss --defaults true`
- Remove zone.js from package.json, remove node_modules & package-lock, run pnpm i (use pnpm)
 pnpm i --save-dev prettier
- Copy .prettierrc.json, test that it works
- `ng add @angular-eslint/schematics --skip-confirmation`
- Go through all files, re-save them, remove extra comments or unneeded data
- Change indent to 4 characters for *.ts in editorconfig, leave 2 for everything else
- Update favicon
- Use OnPush in `app.component.ts`, remove title field, adjust title in `index.html`
- Create Dockerfile, update Compose file (read backend/README for ci/compose instructions)
  - Including Nginx config file
- Copy/install shared Framework files? (auth module / http)
- (later) ng generate config karma (or use this template) and install karma-sabarivka-reporter
