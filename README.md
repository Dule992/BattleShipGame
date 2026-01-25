# Battleship E2E Test Automation

End-to-end test automation project for the online game [Battleship](http://en.battleship-game.org/).

The goal of this project is to simulate a **full Battleship game against a random opponent** and verify that:

- Simulate a full Battleship game against a random opponent and verify:
  - The test passes only when the game ends in `Victory`.
  - Any other outcome (defeat, opponent left, connection lost, timeout) fails the test with a clear reason.
  - The framework uses a non-trivial move strategy (Hunt & Target).
  - The implementation follows production-grade practices: clear architecture, configuration management, logging, reporting, and CI/CD.

Tech stack
- `Language:` C# (.NET 9)
- `Test Framework:` NUnit
- `Browser Automation:` Microsoft Playwright for .NET
- `Logging:` Serilog (console + file)
- `Reporting:` Allure (NUnit adapters + CLI single-file report generation)
- `CI/CD:` GitHub Actions — workflow: `.github/workflows/battleship-tests.yml`

---

## Project Structure

```text
Battleship.Automation/
  src/
    Battleship.Core/              # Game domain logic (board model, strategy, enums)
      Board/
      Strategy/
      Game/
    Battleship.UI/                # Web UI abstraction
      Pages/
      Services/
    Battleship.Tests/             # Test project
      Config/
        Enums/
         BrowserType.cs            # Supported browsers enum
        AllureConfig.cs          # Allure configuration class
        GameConfig.cs            # Game-specific configuration class
        PlaywrightConfig.cs      # Playwright-specific configuration class
        TestConfiguration.cs      # Root configuration class
        appsettings.json          # Configuration (BaseUrl, timeouts, Playwright, logging, Allure)
      Fixtures/
        PlaywrightFixture.cs      # Global Playwright setup/teardown
      Features/
        BattleshipFullGameTests.cs # Full game E2E test
      StepDefinitions/
        BattleShipGameSteps.cs    # Step definitions for game actions
        Hooks.cs                  # Test setup/teardown hooks
      allureConfig.json           # Allure configuration 
  .github/
    workflows/
      battleship-tests.yml        # GitHub Actions CI workflow
    README.md
```

## Reporting

Allure single-file report (how to generate & publish)
- Ensure your test run writes results to `./allure-results`.
- Generate a single-file HTML report locally or in CI:
  - `npm i -g allure-commandline`
  - `allure generate ./allure-results -o ./allure-report --clean --single-file`
- Verify output (CI debug): `ls -la ./allure-report` — you should see the generated artifact (`index.html` or single-file report).
- Publish to GitHub Pages:
  - Use `peaceiris/actions-gh-pages@v4` in your workflow with `publish_dir: allure-report`.
  - For first-ever deploy either set `force_orphan: true` or create an initial `gh-pages` branch so future runs can update history.
  - To preserve history, copy `./gh-pages/history` into `./allure-results/history` before generating the report.