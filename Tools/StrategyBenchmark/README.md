# Strategy Benchmark Utility

A tool to test the Wordle recommendation engine by simulating games and recording performance.

## Usage

```bash
cd Tools/StrategyBenchmark
dotnet run <start_game_number> <end_game_number> [hard_mode]
```

### Examples

Test games 1-100 in Hard mode:
```bash
dotnet run 1 100 hard
```

Test games 1-100 in Normal mode:
```bash
dotnet run 1 100
```

Test all historical games (1-1643):
```bash
dotnet run 1 1643 hard
```

## How It Works

1. Loads word lists and the recommendation engine
2. For each game in the specified range:
   - Gets the answer word from `used-words.csv`
   - Simulates playing Wordle by:
     - Getting recommendations from the engine
     - Using the top recommendation as the guess
     - Simulating the result (Green/Yellow/White feedback)
     - Repeating until the word is found or 6 guesses used
3. Records the number of guesses for each game
4. Outputs statistics and saves detailed results to CSV

## Output

The utility provides:

- **Console Summary:**
  - Games played, won, lost
  - Win percentage
  - Average guesses (for wins only)
  - Guess distribution (1-6 guesses + failures)
  - List of failed games with guess sequences

- **CSV File:**
  - `benchmark_results_{start}_{end}_{mode}.csv`
  - Contains: GameNumber, Answer, Won, GuessCount, Guesses
  - Example: `1,rebut,True,3,"salet,freit,rebut"`

## Example Output

```
Benchmarking recommendation engine:
  Games: 1 to 10
  Mode: HARD

✓ Second-word cache loaded
✓ Loaded 1644 used words

Progress: 10/10 games...

============================================================
RESULTS
============================================================

Games Played: 10
Games Won: 10 (100.0%)
Games Lost: 0
Average Guesses (wins only): 3.50

Guess Distribution:
  1:    0 (  0.0%)
  2:    0 (  0.0%)
  3:    5 ( 50.0%) █████████████████████████
  4:    5 ( 50.0%) █████████████████████████
  5:    0 (  0.0%)
  6:    0 (  0.0%)

Detailed results saved to: benchmark_results_1_10_hard.csv
```

## Use Cases

- **Algorithm Testing:** Evaluate recommendation engine performance
- **Optimization:** Compare different strategies (Hard vs Normal mode)
- **Historical Analysis:** Test against all past Wordle games
- **Regression Testing:** Ensure changes don't degrade performance
