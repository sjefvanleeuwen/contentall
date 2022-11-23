using Contentall.Api.Services.AccountServices;
using Contentall.Api.Services.AccountServices.Helpers;
using Contentall.Data.Provider.Abstractions;
using Contentall.Security.Abstractions.Entities;
using Contentall.Security.Abstractions.Models.Accounts;
using HotChocolate.AspNetCore.Authorization;
using System.Security.Claims;

public class Query
{
    [UsePaging]
    [UseFiltering]
    [Authorize]
    public IQueryable<Account> GetPersons([Service] EntitiesContainer container) => container.GetQueryableEntities<Account>();

    [Authorize]
    public Account? GetMe(ClaimsPrincipal claimsPrincipal, [Service] EntitiesContainer container)
    {
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        return container.GetQueryableEntities<Account>().Where(p => p.Id == userId).Single();
    }
}
public class Person
{
    public Person(string id, string FirstName, string LastName)
    {
        id = id;
        this.FirstName = FirstName;
        this.LastName = LastName;
    }

    public Person() { }
    public string id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

}

public class AccountMutations
{
    private readonly IAccountService accountService;

    public AccountMutations(IAccountService accountService)
    {
        this.accountService = accountService;
    }

    public async Task<CaptchaGenerateResponse> GenerateCaptcha()
    {
        return accountService.GenerateCaptcha();
    }



}

public class Mutation
{
    private readonly EntitiesContainer container;

    public Mutation(EntitiesContainer container)
    {
        this.container = container;
    }

    public async Task<Person> AddPersonAsync(Person person)
    {
        container.Insert(person);
        return person;
    }
    public async Task<Person> UpdatePersonAsync(Person person)
    {
        container.Update(person);
        return person;
    }
    public async Task<string> DeletePersonAsync(string id)
    {
        container.Delete<Person>(id);
        return id;
    }

}
