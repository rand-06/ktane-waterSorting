# Generation algorithm redesign (strict pour rule)

## Problem

`canPour(start, end, scrambleRuleset)` only blocked a mismatched-color pour when
`scrambleRuleset == true`. For a real player pour (`scrambleRuleset == false`) the
color-match term is ANDed with `false`, so it's always `false` — meaning a real pour
was never blocked on color mismatch. Result: you could pour black on top of white.

## Fix

1. **Real pour rule** (`canPour`): require destination empty OR destination's top
   color equals source's top color. No more `scrambleRuleset` flag — there's only
   one pour rule now, and it's the strict one.
2. **Generation**: the old scramble did single-unit reverse-pours that *required*
   mismatched colors (the opposite of the real rule) to create a mixed board. That's
   no longer consistent with the strict real pour rule and isn't reused.

   New approach — deal, don't pour:
   - Build the solved-state color multiset (same per-tube coloring as before:
     `tubesAmount` tubes, tube `i` filled with `sectors` copies of color `i % colors + 1`).
   - Flatten to one list, Fisher-Yates shuffle it.
   - Re-chunk into `tubesAmount` tubes of `sectors` entries each; append
     `emptiesAmount` empty tubes (unchanged from before).
   - Reject/reshuffle if `doneShuffling()` fails (board already trivially sorted,
     i.e. every tube's adjacent segments already differ is the *failure* — actually
     `doneShuffling` means "no adjacent duplicates anywhere", used here as "not a
     boring/degenerate shuffle"; kept as-is from the previous implementation).
   - Bail out after a bounded number of attempts (mirrors the old `start == -1`
     escape hatch) so a pathological settings combo can't infinite-loop.

   Since the shuffle only permutes the exact multiset used to build the solved
   state, and `emptiesAmount` is always clamped to at least 2, the resulting board
   is always solvable under the strict pour rule (classic "ball sort with a spare
   tube" guarantee) without ever performing a cross-color pour during generation.
