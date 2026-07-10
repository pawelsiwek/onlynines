using OnlyNines.Core;
using Xunit;

namespace OnlyNines.Core.Tests;

public class ResourceGraphParserTests
{
    [Fact]
    public void Json_Array_Parses()
    {
        var list = ResourceGraphParser.Parse("""
            [ { "id": "/s/1", "name": "vm1", "resourceGroup": "rg", "type": "Microsoft.Compute/virtualMachines",
                "location": "westeurope", "zones": ["1"], "tier": "Standard" } ]
            """);
        var r = Assert.Single(list);
        Assert.Equal("microsoft.compute/virtualmachines", r.Type);
        Assert.Equal("1", r.Attributes["zoneCount"]);
        Assert.Equal("Standard", r.Attributes["tier"]);
    }

    [Fact]
    public void Json_AzGraphDataWrapper_Parses()
    {
        var list = ResourceGraphParser.Parse("""
            { "count": 1, "data": [ { "id": "/s/1", "name": "db1", "resourceGroup": "rg",
              "type": "microsoft.sql/servers/databases", "zr": true } ] }
            """);
        var r = Assert.Single(list);
        Assert.Equal("true", r.Attributes["zr"]);
    }

    [Fact]
    public void Csv_WithQuotedJsonCells_Parses()
    {
        var csv =
            "id,name,resourceGroup,type,location,zones,sku,ha\n" +
            "/s/1,pg-prod,rg-app,microsoft.dbforpostgresql/flexibleservers,westeurope,\"[\"\"1\"\",\"\"2\"\"]\",\"{\"\"name\"\":\"\"Standard_D2ds\"\",\"\"tier\"\":\"\"GeneralPurpose\"\"}\",ZoneRedundant\n";
        var list = ResourceGraphParser.Parse(csv);
        var r = Assert.Single(list);
        Assert.Equal("microsoft.dbforpostgresql/flexibleservers", r.Type);
        Assert.Equal("2", r.Attributes["zoneCount"]);
        Assert.Equal("GeneralPurpose", r.Attributes["skuTier"]);
        Assert.Equal("ZoneRedundant", r.Attributes["ha"]);
    }

    [Fact]
    public void Csv_EmptyAndNullCells_AreSkipped()
    {
        var csv =
            "id,name,resourceGroup,type,zr\n" +
            "/s/1,st1,rg,microsoft.storage/storageaccounts,null\n" +
            "/s/2,st2,rg,microsoft.storage/storageaccounts,\n";
        var list = ResourceGraphParser.Parse(csv);
        Assert.Equal(2, list.Count);
        Assert.All(list, r => Assert.False(r.Attributes.ContainsKey("zr")));
    }

    [Fact]
    public void Garbage_ThrowsInvalidData()
    {
        Assert.ThrowsAny<Exception>(() => ResourceGraphParser.Parse("not,a\nvalid input"));
    }
}
