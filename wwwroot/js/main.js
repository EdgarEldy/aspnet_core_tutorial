// Handles ajax requests
// Get products by category id
$(function () {
    $('#CategoryId').on('change', function () {
        var id = $(this).val();
        //alert(id);
        $.get('/Orders/GetProducts', {id: id}, function (data) {
            $('#ProductId').empty();
            $.each(data, function (index, row) {
                $('#ProductId').append("<option value='" + row.Id + "'>" + row.ProductName + "</option>")
            });
        });
    });
});