# MagicGatherer
This project is a website for tracking your Magic: The Gathering cards, using the prices from the CardKingdom website. The goal was to have a dedicated server for scraping the website periodically and updating the database which will be used by my API. The clients will interact with the frontend server to send requests over to the API.

## Links to other repositories:
- Frontend: [https://github.com/burgerax5/MagicCollector_Frontend](https://github.com/burgerax5/MagicGatherer_Frontend)
- Web Scraper: https://github.com/burgerax5/CardKingdomWebScraper

| *Browse Cards* |
| :--: |
|![AllCards](https://github.com/user-attachments/assets/47e34233-d055-4596-babc-a1509c93c831) |

| *View Your Collection* |
| :--: |
| ![MyCards](https://github.com/user-attachments/assets/a57d7745-1c49-4b02-ab0c-19507f6076d5) |

| *Add cards in different conditions* |
| :--: |
| ![image](https://github.com/user-attachments/assets/ae24f711-b0e2-4f37-af37-1faf9870d322) |

One thing I am proud of is not giving up on the project. I tried working with a bunch of new technologies and struggled my way through. I'm not satisfied with the current state of the project as it is very buggy but I intend on improving it in the future.

## Basic Requirements:
I have checked all the basic requirements except for using a styling library. I didn't read the requirements properly, and in hindsight, BIG MISTAKE.

## Advanced Requirements:
I tried to containerize my applications but came across some issues.
- Unit testing for my web scraper & web API
- Redux for state management
- Switching between light/dark mode
- Deploy on Azure

# Setup
First you want to add the connection string to your database in `appsettings.json` and replace my existing value from the `"DefaultConnection"` property. To connect to the database, open Visual Studio and go to the Package Manager Console:
```
Add-Migration InitialCreate
Update-Database
```
Next, you will need to setup the Redis server for caching. IMO the easiest way to do this is using a Redis container in Docker but there are probably other ways.
```
docker pull redis
docker run -d --name redis-stack-server -p 6379:6379 redis/redis-stack-server:latest
```
Assuming all the packages are installed and you are connected to the database, and also have the Redis server running then you should be ready to run the program using IIS (My docker Container doesn't work)

**DISCLAIMER: You probably will just see empty data since all the data is from the web scraper** https://github.com/burgerax5/CardKingdomWebScraper

I don't have Swagger setup for displaying the API endpoints but you can refer to my controllers to test them out :(
