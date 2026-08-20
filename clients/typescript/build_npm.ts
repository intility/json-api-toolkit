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
    name: '@intility/jsonapi-ts-tools',
    version: Deno.args[0] || '0.0.0',
    description:
      'jsonapi-ts-tools is a TypeScript library for working with JSON:API.',
    license: 'MIT',
    repository: {
      type: 'git',
      url: 'git+https://github.com/intility/jsonapi-ts-tools.git',
    },
    bugs: {
      url: 'https://github.com/intility/jsonapi-ts-tools/issues',
    },
  },
  postBuild() {
    Deno.copyFileSync('README.md', 'dist/README.md');
  },
});
