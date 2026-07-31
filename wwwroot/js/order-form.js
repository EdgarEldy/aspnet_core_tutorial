// Drives the Category -> Product cascading dropdown on the Orders Create/Edit forms: selecting
// a category loads only that category's products via AJAX (no page reload), and selecting a
// product fills in its unit price and recalculates the total (Quantity * UnitPrice) live.
// The server always recomputes and trusts its own total on save; these fields are read-only
// preview inputs, not part of the posted form data.
$(function () {
    var $categorySelect = $('#CategoryId');
    var $productSelect = $('#ProductId');
    var $unitPriceDisplay = $('#UnitPriceDisplay');
    var $totalDisplay = $('#TotalDisplay');
    var $quantity = $('#Quantity');

    if ($categorySelect.length === 0 || $productSelect.length === 0) {
        return;
    }

    var productsByCategory = [];

    function unitPriceOf(productId) {
        var match = productsByCategory.filter(function (product) {
            return String(product.id) === String(productId);
        })[0];
        return match ? match.unitPrice : null;
    }

    function updateUnitPriceAndTotal() {
        var unitPrice = unitPriceOf($productSelect.val());
        var quantity = parseInt($quantity.val(), 10) || 0;

        if (unitPrice === null) {
            $unitPriceDisplay.val('');
            $totalDisplay.val('');
            return;
        }

        $unitPriceDisplay.val(unitPrice.toFixed(2));
        $totalDisplay.val((unitPrice * quantity).toFixed(2));
    }

    function loadProducts(categoryId, selectedProductId) {
        if (!categoryId) {
            productsByCategory = [];
            $productSelect.empty().append('<option value="">-- Select a product --</option>');
            $productSelect.prop('disabled', true);
            updateUnitPriceAndTotal();
            return;
        }

        $.get('/Orders/GetProducts', { categoryId: categoryId }, function (data) {
            productsByCategory = data;
            $productSelect.empty().append('<option value="">-- Select a product --</option>');
            $.each(data, function (index, product) {
                var option = $('<option></option>').val(product.id).text(product.productName);
                if (selectedProductId && String(product.id) === String(selectedProductId)) {
                    option.prop('selected', true);
                }
                $productSelect.append(option);
            });
            $productSelect.prop('disabled', false);
            updateUnitPriceAndTotal();
        });
    }

    $categorySelect.on('change', function () {
        loadProducts($(this).val(), null);
    });

    $productSelect.on('change', updateUnitPriceAndTotal);
    $quantity.on('input', updateUnitPriceAndTotal);

    // Initial state: a category/product may already be selected (Edit, or Create redisplayed
    // after a validation error), so load that category's products up front instead of leaving
    // the Product dropdown empty until the user touches Category again.
    var initialCategoryId = $categorySelect.val();
    var initialProductId = $productSelect.val();
    if (initialCategoryId) {
        loadProducts(initialCategoryId, initialProductId);
    } else {
        $productSelect.prop('disabled', true);
    }
});
