# Policy Pack Schema Starter Set (Draft 2020-12)

These JSON Schema files validate Anseo Connect policy packs.

## How to use
- Put these files into your repo at: `policy-packs/schema/`
- Validate each policy pack JSON using a JSON Schema validator (CI + runtime).
- Root schema: `policy-pack.schema.json`
- Module schemas are referenced via relative `$ref`.

## Notes
- The root schema allows extra top-level properties (`additionalProperties: true`) to remain forward-compatible.
- Module schemas are intentionally stricter (`additionalProperties: false`) to catch typos early.
