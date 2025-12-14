# Wordle Recommendations Optimization Analysis

## Problem Statement

Normal mode recommendations are very slow, especially early in the game when there are few green or yellow letters. Hard mode is much faster due to constraints limiting the candidate pool.

## Performance Issue Analysis

### Why Normal Mode is Slow

**Hard Mode:**
- Heavily constrained by previous guesses (must use green/yellow letters)
- Evaluates only ~50-200 valid candidates
- Fast even with few clues

**Normal Mode (Current):**
- Evaluates ALL ~13,000 words (2,300 answers + 10,700 guesses)
- With few clues and many possible answers (~2,300)
- Potentially **13,000 candidates × 2,300 possible answers = ~30 million pattern calculations**
- Early game is worst case scenario

---

## Optimization Strategies

### 🚀 Option 1: Smart Candidate Filtering (Biggest Impact)

**Idea:** Don't evaluate all 13,000 words. Filter based on game state.

**Strategy:**
```
Early game (>200 possible answers):
  → Only evaluate ~1,500 high-quality words
  → Words with common letters (E,A,R,S,T,O)
  → High letter diversity, no repeats
  → 8-10x faster

Mid game (20-200 possible answers):
  → All answer words + top guess-only words
  → ~3,000-4,000 candidates
  → 3-4x faster

Late game (<20 possible answers):
  → Evaluate all words (already fast)
```

**Expected Speedup:** 5-10x for early game

---

### 🎯 Option 2: Sample-Based Entropy (Good Combo with #1)

**Idea:** When there are many possible answers, calculate entropy against a sample instead of all.

**Strategy:**
```
If possibleAnswers.Count > 500:
  → Randomly sample 500 answers
  → Calculate entropy against sample
  → 3-5x faster, ~95% accuracy
```

**Trade-off:** Slightly less optimal recommendations, but much faster

---

### 📊 Option 3: Two-Pass Strategy

**Idea:** Quick scoring pass, then accurate scoring on survivors.

**Strategy:**
```
Pass 1: Fast heuristic score (letter frequency, diversity)
  → Keep top 20% (~2,600 words)

Pass 2: Full entropy calculation
  → On remaining ~2,600 words
  → 3-4x faster overall
```

---

### 💾 Option 4: Pre-computed Quality Tiers

**Idea:** Create static tiers of guess quality offline.

**Strategy:**
```
Tier 1 (Excellent): ~500 words - always evaluate
Tier 2 (Good): ~2,000 words - evaluate if >100 answers
Tier 3 (Okay): ~5,000 words - evaluate if <50 answers
Tier 4 (Poor): Rest - only if <10 answers
```

---

### ⚡ Option 5: Progressive Results

**Idea:** Show quick results, refine in background.

**Strategy:**
```
1. Evaluate top 500 candidates (fast)
2. Show top 3 recommendations immediately
3. Continue evaluating rest in background
4. Update if better found

Perceived: Instant
Actual: Same time, but UI responsive
```

---

## Pre-computation Analysis

### Single Guess in Normal Mode - Permutation Breakdown

#### Theoretical Maximum

**Pattern combinations per guess word:**
- Each of 5 positions can be: Green (G), Yellow (Y), or White (W)
- Total patterns = 3^5 = **243 possible patterns**

**For one specific starting word (e.g., "salet"):**
- 243 possible feedback patterns
- But many are impossible/never occur with real words
- **Realistic: ~50-150 actual patterns** that occur

### Pre-computation Scenarios

#### Option A: Just Top Starting Words (Recommended)

```
Top 10 starting words (salet, raile, soare, etc.)
× ~100 realistic patterns each
= ~1,000 pre-computed second-word recommendations

Storage: ~50-100 KB JSON
Lookup: Instant
Coverage: 95%+ of actual games
```

#### Option B: Common Starting Words

```
Top 100 starting words
× ~100 realistic patterns each
= ~10,000 pre-computed scenarios

Storage: ~500 KB - 1 MB
Lookup: Instant
Coverage: 99%+ of games
```

#### Option C: All Possible First Guesses

```
~13,000 possible first words
× ~100 realistic patterns each
= ~1.3 million pre-computed scenarios

Storage: ~50-100 MB
Computation time: Hours/days offline
Lookup: Very fast
Coverage: 100%
```

---

## Storage Size Estimates

**Per pre-computed entry:**
```json
{
  "firstGuess": "salet",
  "pattern": "WYYWW",
  "recommendations": ["arose", "irate", "react"]
}
```
~50-100 bytes per entry

**Full coverage examples:**
- Top 10 words: ~50 KB
- Top 100 words: ~500 KB
- All words: ~65 MB

---

## Estimated Performance Impact

| Scenario | Current | Optimized | Speedup |
|----------|---------|-----------|---------|
| Early game (0-1 guesses) | ~5-10 sec | ~0.5-1 sec | **10x** |
| Mid game (2-3 guesses) | ~2-3 sec | ~0.5-1 sec | **4x** |
| Late game (4+ guesses) | ~0.5-1 sec | ~0.3-0.5 sec | **2x** |

---

## Recommended Implementation Plan

### Phase 1: Smart Candidate Filtering + Sampling

**Combine Options 1 + 2 for immediate gains**

```csharp
private List<string> GetSmartNormalModeCandidates(List<Guess> guesses, List<string> possibleAnswers)
{
    int answerCount = possibleAnswers.Count;

    if (answerCount > 200)  // Early game
    {
        // Only high-quality pre-filtered words (~1,500)
        return _highQualityCandidates;
    }
    else if (answerCount > 20)  // Mid game
    {
        // All possible answers + top guess-only words
        return _allWords
            .Where(w => w.IsPossibleAnswer || _topGuessWords.Contains(w.Word))
            .Select(w => w.Word)
            .ToList();
    }
    else  // Late game
    {
        // Fast enough to check everything
        return _allWords.Select(w => w.Word).ToList();
    }
}

private double CalculateExpectedInformation(string guess, List<string> possibleAnswers)
{
    // Sample if too many answers
    var answersToEvaluate = possibleAnswers.Count > 500
        ? SampleRandomly(possibleAnswers, 500)
        : possibleAnswers;

    // ... existing entropy calculation
}
```

**Expected Impact:**
- 5-10x speedup for early game
- 2-5x speedup for mid game
- Maintains high accuracy (~95%)

---

### Phase 2: Pre-computed Second Word Cache

**Cache recommendations for top starting words**

#### Immediate (Phase 2A):
- Cache for our 3 recommended starters: salet, raile, saner
- ~300 scenarios total
- ~15 KB storage
- Covers 80% of users
- Second word: Instant

#### Later (Phase 2B):
- Expand to top 20 starting words
- ~2,000 scenarios
- ~100 KB storage
- Covers 98% of users

#### Implementation:
```json
{
  "salet": {
    "WYYWW": ["arose", "irate", "react"],
    "WYGWW": ["intro", "north", "sport"],
    ...
  },
  "raile": {
    ...
  }
}
```

**Expected Impact:**
- Second guess: Instant (if first word is cached)
- Third+ guesses: Use Phase 1 optimizations

---

### Phase 3: Progressive Results (Optional)

**Show instant results, refine in background**

- Evaluate subset of candidates quickly
- Display top 3 immediately
- Continue calculation in background
- Update if better words found

**Expected Impact:**
- Perceived instant response
- Actual calculation time same
- Better UX

---

## Questions to Consider

1. **Accuracy vs Speed:** Are you okay with ~95% accuracy for 10x speed? (Sample-based approach)
2. **Pre-computation:** Should we generate the high-quality word list offline or compute it once on startup?
3. **Progressive results:** Would you like "instant" results that refine, or wait for accurate results?
4. **Storage:** Is 100 KB - 1 MB acceptable for pre-computed cache files?

---

## Next Steps

1. **Calculate exact pattern counts** for our top starting words (salet, raile, saner)
2. **Implement Phase 1**: Smart candidate filtering + sampling
3. **Test performance** improvements
4. **Generate Phase 2A cache**: Pre-compute for top 3 starting words
5. **Implement cache lookup** with fallback to live calculation

---

## Technical Notes

### Async Considerations

Current `CalculateRecommendations()` is already async, but blocks on synchronous `GetRecommendations()`.

**Option:** Wrap in Task.Run for better UI responsiveness
```csharp
recommendations = await Task.Run(() =>
    StrategyService.GetRecommendations(guesses, isHardMode, topN: 5)
);
```

**Limitation:** Blazor WebAssembly is single-threaded, so true parallelism is limited, but can still yield control to UI.

---

## References

- Current implementation: `Services/WordleStrategyService.cs`
- Recommendations UI: `Pages/Home.razor`
- Starting words cache: `wwwroot/starting-words.json`
- Test for generating cache: `Tests/WordleStrategyServiceTests.cs::GenerateStartingWordsExcludingUsed`
