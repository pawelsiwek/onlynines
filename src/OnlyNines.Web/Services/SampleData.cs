namespace OnlyNines.Web.Services;

/// <summary>Canned Resource Graph output used by "Try with sample data" and /report/sample.</summary>
public static class SampleData
{
    public const string Json =
        """
        [
          { "name": "vm-web-01", "resourceGroup": "rg-shop-prod", "type": "microsoft.compute/virtualmachines", "location": "westeurope", "zones": null },
          { "name": "sql-orders-prod", "resourceGroup": "rg-shop-prod", "type": "microsoft.sql/servers/databases", "location": "westeurope", "zones": null, "zr": false },
          { "name": "st-assets-lrs", "resourceGroup": "rg-shop-prod", "type": "microsoft.storage/storageaccounts", "location": "westeurope", "replication": "Standard_LRS" },
          { "name": "redis-session-c1", "resourceGroup": "rg-shop-prod", "type": "microsoft.cache/redis", "location": "westeurope", "zr": false },
          { "name": "appgw-edge-01", "resourceGroup": "rg-shop-prod", "type": "microsoft.network/applicationgateways", "location": "westeurope", "zones": ["1"] },
          { "name": "kv-shop-secrets", "resourceGroup": "rg-shop-prod", "type": "microsoft.keyvault/vaults", "location": "westeurope" },
          { "name": "func-webhooks-consumption", "resourceGroup": "rg-shop-prod", "type": "microsoft.web/sites", "kind": "functionapp", "location": "westeurope" },
          { "name": "eventgrid-topic-orders", "resourceGroup": "rg-shop-prod", "type": "microsoft.eventgrid/topics", "location": "westeurope" }
        ]
        """;
}
