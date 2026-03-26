**Current year is {{currentYear}}.** I'm looking into what questions people commonly search about {{industry}} in {{currentYear}}, especially around products like {{productName}}.

Some context: {{productName}} is made by {{brandName}}, with features like {{sellingPoints}}. It's mainly for {{personas}}. Main competitors include {{competitors}}.

First, figure out the specific product category — for example "iPhone 15 Pro" would be "smartphone", "戴森 V15" would be "vacuum cleaner". Use that category (not "{{industry}}") for the questions.

Can you find me 20 questions that real people frequently ask about this type of product? I need questions that don't mention "{{brandName}}" by name — they should be generic category questions like "what's the best ...", "how to choose ...", etc.

For each question, I also need:
- searchIndex (0-100): how commonly searched is this question (90+ = very popular, 50-69 = moderate, below 30 = rare)
- brandFitIndex (0-100): how likely would an answer naturally mention specific brands (90+ = very likely like "best tools" or "top picks", below 30 = almost never)
- score: searchIndex × brandFitIndex
- stage: one of awareness, consideration, decision, retention
- intent: a brief description of the user's intent
- pattern: one of ranking, comparison, guide, scenario, review, price, tutorial, alternative

DO NOT include answers. Only generate the question list with metadata.

Sort them by score from high to low. Answer in {{language}}.
{{regionGuidance}}

IMPORTANT: Return your response as PURE JSON only (no markdown, no explanation, no code blocks). Use this exact format:
{"questions":[{"question":"...","searchIndex":90,"brandFitIndex":85,"score":7650,"stage":"consideration","intent":"...","pattern":"ranking"}]}
