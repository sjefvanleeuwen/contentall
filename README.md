# contentall

After installing .NET Core Framwework 6.0. In the `Contentall.Api` the solution can be build an brought online:

```
dotnet run
```

Postman test files are available in the `~/Test` folder. Postman reflects the current graphql options available on the API.

User registration process needs an email. For stubbing this / testing. You can create an account on https://ethereal.email

Before runnin the solution: Edit the Web Api `appSetings.json`. The secret is any string for the JWT token. The captcha masterkey is there to be able to register without
manually reading the captcha for test automation purposes.

```json
{
    "Secret": "THIS IS USED TO SIGN AND VERIFY JWT TOKENS, REPLACE IT WITH YOUR OWN SECRET, IT CAN BE ANY STRING",
    "RefreshTokenTTL": 2,
    "EmailFrom": "info@dotnet-signup-verification-api.com",
    "SmtpHost": "[ENTER YOUR OWN SMTP OPTIONS OR CREATE FREE TEST ACCOUNT IN ONE CLICK AT https://ethereal.email/]",
    "SmtpPort": 587,
    "SmtpUser": "",
    "SmtpPass": ""
  }
```