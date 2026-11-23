using System.Linq.Expressions;
using Api.Application.People;
using Api.Domain.People;

namespace Api.Infrastructure.Persistence.Repositories;

/// <summary>The list's search, filters and sorting, as expressions EF Core runs in SQL.</summary>
internal static class PeopleQueries
{
    /// <summary>The people matching every search word and filter.</summary>
    public static IQueryable<Person> Matching(this IQueryable<Person> query, PersonSearch search)
    {
        foreach (var term in search.Terms)
        {
            // Matches whenever Score(term) > 0: a word-start match is also a match in the name.
            var code = term.ToUpperInvariant();
            query = query.Where(p =>
                p.SearchName.Contains(term) || p.SearchText.Contains(term) || p.Country == code);
        }
        if (search.Country is { } country)
        {
            query = query.Where(p => p.Country == country);
        }
        if (search.Age is { } range)
        {
            query = query.Where(p => p.Age >= range.Min);
            if (range.Max is { } max) query = query.Where(p => p.Age <= max);
        }
        foreach (var interest in search.Interests)
        {
            query = query.Where(p => p.Interests.Contains(interest));
        }
        return query;
    }

    /// <summary>In the search's order; ties go to the newest.</summary>
    public static IOrderedQueryable<Person> SortedBy(this IQueryable<Person> query, PersonSearch search) =>
        search.Sort switch
        {
            PersonSort.OldestFirst => query.OrderBy(p => p.Id),
            PersonSort.NameAscending => query.OrderBy(p => p.SearchName).ThenByDescending(p => p.Id),
            PersonSort.NameDescending => query.OrderByDescending(p => p.SearchName).ThenByDescending(p => p.Id),
            PersonSort.AgeAscending => query.OrderBy(p => p.Age).ThenByDescending(p => p.Id),
            PersonSort.AgeDescending => query.OrderByDescending(p => p.Age).ThenByDescending(p => p.Id),
            PersonSort.CountryAscending => query.OrderBy(p => p.CountryName).ThenByDescending(p => p.Id),
            PersonSort.CountryDescending => query.OrderByDescending(p => p.CountryName).ThenByDescending(p => p.Id),
            PersonSort.BestMatch when search.Terms.Count > 0 => query
                .OrderByDescending(TotalScore(search.Terms))
                .ThenBy(p => p.SearchName)
                .ThenByDescending(p => p.Id),
            _ => query.OrderByDescending(p => p.Id),
        };

    /// <summary>
    /// How well one search word matches a person: 3 = a word of the name starts with it,
    /// 2 = it's elsewhere in the name, 1 = it's in another field or is the country code, 0 = no match.
    /// </summary>
    private static Expression<Func<Person, int>> Score(string term)
    {
        var wordStart = " " + term;
        var code = term.ToUpperInvariant();
        return p =>
            (" " + p.SearchName.Replace("-", " ").Replace("'", " ").Replace("’", " ")).Contains(wordStart) ? 3
            : p.SearchName.Contains(term) ? 2
            : p.SearchText.Contains(term) || p.Country == code ? 1
            : 0;
    }

    /// <summary>The sum of several scores, as one expression EF Core can translate.</summary>
    private static Expression<Func<Person, int>> TotalScore(IEnumerable<string> terms)
    {
        var person = Expression.Parameter(typeof(Person), "p");
        Expression total = Expression.Constant(0);
        foreach (var term in terms)
        {
            var score = Score(term);
            total = Expression.Add(total, new ReplaceParameter(score.Parameters[0], person).Visit(score.Body));
        }
        return Expression.Lambda<Func<Person, int>>(total, person);
    }

    private sealed class ReplaceParameter(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}
