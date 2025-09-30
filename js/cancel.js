$(function() {
    //入金申請キャンセルダイアログ
    $(".dialog-depositAddCancel").dialog({
        autoOpen: false,
        modal: true,
        resizable: false,
        overlay: {
            backgroundColor: '#fff',
            opacity: '0.8'
        },
        title:msg[0],
        open: function() {
            var window_height = $(window).height();
            $(this).css("max-height", window_height - 160);

            current_scroll = $( window ).scrollTop();

            $("body").css( {
                position: "fixed",
                width: "100%",
                top: -1 * current_scroll
            });
            $(".header").addClass("header-under");
        },
        close: function() {
            $("body").attr({style: ""});
            $("html, body").prop({scrollTop: current_scroll});
            $(".header").removeClass("header-under");
        }
    });

    // 入金申請キャンセルボタンクリック
    $(".btn-depositAddCancel").on("click", function() {
        $(".dialog-depositAddCancel").dialog("open");
    });

    // 入金申請キャンセルのキャンセル
    $(".btn-dialogCancel").on("click", function() {
        $(this).parents(".dialog").dialog("close");
    });

    // 入金申請キャンセル実行
    $(".btn-dialogOk").on("click", function() {
        $(this).parents(".dialog").dialog("close");
        $(".loading").show();
        $(".dialog_form").submit();
    });
});
