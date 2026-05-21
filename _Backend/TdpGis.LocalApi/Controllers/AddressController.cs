using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TdpGis.LocalApi.DatabaseServices;
using TdpGis.LocalApi.Models;

namespace TdpGis.LocalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AddressController(IOptions<SearchFeature> option, IGisMongoQueryRepository repos) : ControllerBase
{
    [HttpGet("{searchTerm:alpha:minlength(3)}")]
    public async Task<IActionResult> Get(string searchTerm)
    {
        var searchObject = option.Value;
        var results = await repos.SearchAsync(searchObject.ConnectionString, searchObject.DatabaseName,
            searchObject.CollectionName, searchObject.SearchField, searchTerm, searchObject.IncludeFields, 50);


        return Ok(results);
    }

    [HttpGet("paged/{searchTerm:alpha:minlength(3)}/{pageIndex:int}/{pageSize:int=50}")]
    public async Task<IActionResult> Get(string searchTerm, int pageIndex, int pageSize)
    {
        var searchObject = option.Value;
        var results = await repos.SearchPagedAsync(searchObject.ConnectionString, searchObject.DatabaseName,
            searchObject.CollectionName, searchObject.SearchField, searchTerm, searchObject.IncludeFields, pageIndex,
            pageSize);


        return Ok(results);
    }
}