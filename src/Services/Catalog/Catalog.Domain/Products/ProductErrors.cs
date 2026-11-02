using BuildingBlocks.Common.Results;

namespace Catalog.Domain.Products;

public static class ProductErrors
{
    public static readonly Error InvalidPrice =
        new("PRODUCT_INVALID_PRICE", "Price 0 se zyada hona chahiye.");

    public static readonly Error InvalidName =
        new("PRODUCT_INVALID_NAME", "Naam khali nahi ho sakta.");

    public static readonly Error InvalidSku =
        new("PRODUCT_INVALID_SKU", "SKU khali nahi ho sakta.");
}
