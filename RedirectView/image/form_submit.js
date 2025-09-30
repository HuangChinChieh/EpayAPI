$(function() {
    $('.formSubmit').click(function () {
        var form = $(this).parents('form');
        form.submit();
    });

    $('.submitForm').submit(function () {
        $(".formSubmit").addClass("btn-disable");
        $(".loading").show();
    });
});
