@allure.epic:WebGame
Feature: BattleShipGame
As a player
I want to play Battle Ship Game
And try to win opponent player

@allure.story:PlayGame
Scenario: Playing battle ship game
	Given I open Battle Ship online game
	When I click play button to start game
	Then I will play a game until it's finished with victory
