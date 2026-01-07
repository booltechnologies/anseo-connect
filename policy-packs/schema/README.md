# Policy Pack Schema Starter Set (Patched to match existing packs)

These schemas are Draft 2020-12 and are relaxed in a few places to match the current JSON packs:
- supportsCatalogue.recommendations: supports either `topN` or `rules`
- consentModel.audit: supports current fields (`logFields`, `retainWhileEnrolled`) and optional `enabled/recordDecision`
- consentModel.routingRules: allows rules without `onBlocked/selectChannelOrder` and supports gate variants
- safeguarding.aiConstraints/audit/playbook: allows boolean flags and checklist severity fields

Copy all `*.schema.json` files into `policy-packs/schema/` in your repo.
