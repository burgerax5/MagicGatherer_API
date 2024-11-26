# MagicGatherer
This project is a website for tracking your Magic: The Gathering cards, using the prices from the CardKingdom website. The goal was to have a dedicated server for scraping the website periodically and updating the database which will be used by my API. The clients will interact with the frontend server to send requests over to the API.

Link to website: https://magicgatherer.netlify.app/ (backend server has been shut down)

**DISCLAIMER:** The database goes to sleep after an hour of inactivity so it might take a while for it to resume.

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
I have checked all the basic requirements except for using a styling library.

## Advanced Requirements:
I tried to containerize my applications but came across some issues.
- Unit testing for my web scraper & web API
- Redux for state management
- Switching between light/dark mode
- Deploy on Azure

# Setup
First you want to add the connection string to your database in `appsettings.json`, and ignore the other properties for now. If you don't have an existing database, you can create one using SSMS and you can retrieve the connection string from Visual Studio by clicking _Search_ at the top, and searching for "SQL Server Object Explorer". In the SQL Server Object Explorer, navigate to your database and under _Properties_ you can get the connection string
```
{
  "DbConnection": "MY_DB_CONN_STRING",
  ...
}
```
![image](https://github.com/user-attachments/assets/dc0af435-11ef-4c15-b1a3-0ebd71ff0144)

To connect to the database, use the Package Manager Console in Visual Studio and use the following commands:
```
Add-Migration InitialCreate
Update-Database
```
Secondly, in `Program.cs` _**comment out or delete**_ the code below because you will be using the values in `appsettings.json` and not the Azure Key Vault.
```
builder.Configuration.AddAzureKeyVault(new Uri("https://mtgcardsvault.vault.azure.net/"), new DefaultAzureCredential());
```

Next, you will need to setup the Redis server for caching. IMO the easiest way to do this is using a Redis container in Docker but there are other ways.
```
docker pull redis
docker run -d --name redis-stack-server -p 6379:6379 redis/redis-stack-server:latest
```
Lastly, you will need to allow the origin of your frontend. To do this, go to `Program.cs` and go down to line 68/69 and change the value of `origin` to that of your frontend, after it is run. This is necessary if you want to test the frontend with the API otherwise it'll get blocked by CORS policy.

![image](https://github.com/user-attachments/assets/75db2044-4812-41b5-904a-691ce3f8d97a)

Assuming all the packages are installed and you are connected to the database, and **also have the Redis server running** then you should be ready to run the program.

**DISCLAIMER: You will probably see no data since all the data is scraped** https://github.com/burgerax5/CardKingdomWebScraper

I don't have Swagger setup for displaying the API endpoints but you can refer to my controllers to test them out, or alternatively interact with the API through the frontend.

---
**Optionally**, if you would like to test out the forgot password feature, in `appsettings.json` change the values of the keys shown below. You don't have to use your actual email, you can use the website: https://ethereal.email to generate a test account and use the provided email and password.
![image](https://github.com/user-attachments/assets/0f69d54c-f7e1-4fc6-a49e-de4e128c3610)
```
{
  ...
  "EmailHost": "smtp.ethereal.email",
  "EmailUsername": "YOUR_EMAIL_ADDRESS",
  "EmailPassword": "SENDER_PASSWORD",
}
```
Then go to `/Controllers/UserController.cs` and change the `resetPasswordLink` to be the URL of your frontend server (e.g. $"http://localhost:5173/reset-password?token={resetToken}"). All emails will be sent to the test email, and to view them, make sure to open the mailbox.
