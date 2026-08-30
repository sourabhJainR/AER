import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { decode } from '../dist/index.js';
const repo = new URL('../../..', import.meta.url);
const vectors = JSON.parse(readFileSync(new URL('conformance/binary/v1.json', repo), 'utf8'));
for (const vector of vectors.vectors) assert.notEqual(decode(Uint8Array.from(Buffer.from(vector.hex, 'hex'))), undefined, vector.id);
console.log(`PASS ${vectors.vectors.length} frozen AER-B vectors`);
