SalamandraTA-7B test kit (gomets corpus, 2026-08-04)
====================================================

Tests the BSC SalamandraTA-7B translation model locally against the
sentences from Marina's 2026-07-26 sermon that broke NLLB/Google
("gomet" -> rubber/gum/nino/gummy bear). Nothing here touches EveryTongue.

Steps on Jezer:
  1. Copy this whole folder anywhere (e.g. Desktop).
  2. Run download.cmd   - fetches the model (5.07 GB) + llama.cpp (~52 MB).
                          Re-run if it fails; finished parts are skipped.
  3. Run run-tests.cmd  - tries GPU (Vulkan) first, falls back to CPU.
                          10 test cases; expect a few minutes on GPU,
                          maybe 15-30 min on CPU.
  4. Send back results.txt AND perf.log.

What the cases test:
  S1..S4-bare       each gomet sentence alone - the regime NLLB sees today
  S3-bare-es        the "Un gomet daurat" line that NLLB turned into "un nino"
  S3-ctx1           one prior sentence of context
  PARA-en / PARA-es full real preceding-sermon context, EN and ES
  S2-term / S3-term-es  glossary prompt ("Translate 'gomet' as 'sticker'") -
                    tests the terminology capability, clearly separated from
                    the clean no-glossary cases above

Notes:
  - Output may contain a trailing <|im_end|> or repeated text after the
    translation - harmless, generation is capped at 200 tokens.
  - perf.log holds llama.cpp's speed stats (tokens/sec) - needed to judge
    whether live-caption latency is feasible on this machine.
