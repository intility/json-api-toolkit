import { build, emptyDir } from '@deno/dnt';

await emptyDir('./dist');

await build({
  entryPoints: ['./src/index.ts'],
  outDir: './dist',
  shims: {
    deno: true,
  },
  test: false,
  package: {
    name: '@intility/json-api-client',
    version: Deno.args[0] || '0.1.0',
    publishConfig: {
      access: 'public',
    },
    description:
      'json-api-client is a TypeScript library for working with JSON:API.',
    license: 'MIT',
    repository: {
      type: 'git',
      url: 'git+https://github.com/intility/json-api-toolkit.git',
    },
    bugs: {
      url: 'https://github.com/intility/json-api-toolkit/issues',
    },
  },
  postBuild() {
    Deno.copyFileSync('README.md', 'dist/README.md');
  },
});
