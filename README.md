# Battleship E2E Test Automation

End-to-end test automation project for the online game [Battleship](http://en.battleship-game.org/).

The goal of this project is to simulate a **full Battleship game against a random opponent** and verify that:

- The test **passes only when the game ends in victory**.
- Any other outcome (defeat, opponent left, connection lost, timeout) **fails the test with a clear reason**.
- The framework uses a **non-trivial move strategy (Hunt & Target)** to maximize the probability of winning.
- The implementation follows **commercial-grade standards**: clean architecture, configuration management, logging, reporting, and CI/CD integration.

---

## Tech Stack

- **Language:** C# (.NET 8)
- **Test Framework:** NUnit
- **Browser Automation:** Microsoft Playwright for .NET
- **Logging:** Serilog (console + file)
- **Reporting:** Allure (NUnit + HTML reports)
- **CI/CD:**
  - GitHub Actions workflow: `.github/workflows/battleship-tests.yml`
  - Azure DevOps pipeline: `azure-pipelines.yml`

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
        appsettings.json          # Configuration (BaseUrl, timeouts, Playwright, logging, Allure)
        *.cs                      # Strongly-typed config classes
      Fixtures/
        PlaywrightFixture.cs      # Global Playwright setup/teardown
      Logging/
        TestLogging.cs            # Serilog configuration
      Tests/
        BattleshipFullGameTests.cs
      allureConfig.json           # Allure configuration
  .github/
    workflows/
      battleship-tests.yml        # GitHub Actions CI workflow
  azure-pipelines.yml             # Azure DevOps pipeline definition
  README.md
