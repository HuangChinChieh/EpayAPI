$(function(){
    // iOSでブラウザバックした際にページをリロードする
    window.onpageshow = function(e) {
        if (e.persisted) {
            location.reload();
        }
    };

    $("#spNavi").mmenu({
        position:"right",
        slidingSubmenus:false
    });

    //[PC時]ヘッダースクロール
    var header = $("#header");

    $(window).on("scroll resize load",function(){
        _handleScroll();
    });

    function _handleScroll(){
        var w = $(window).width();
        if(w >= 560){
            header.css("left",-window.scrollX + "px");
        }else{
            header.removeAttr("style");
        }
    }

    //Page Top
    $("a",".pagetop_sp").click(function(){
        $('html,body').animate({ scrollTop: 0 }, 'slow');
        return false;
    });

    // 言語メニュー表示・非表示
    $(".language_selectBox").click(function(event) {
        $(".cart_menu,.accountNavi").hide();
        $(".language_menu").toggle();
        event.stopPropagation();
    });

    userAgent = navigator.userAgent;

    // 画像ラジオボタンタブレット選択処理
    $(".imgList_label").on("mouseover", function() {
        // iPad, tabletじゃない場合
        if ((userAgent.indexOf('iPad') <= -1) && !is_tablet) {
            $(this).addClass("imgList_label-hover");
        }
    }).on("mouseout", function() {
        $(this).removeClass("imgList_label-hover");
    });

    // カートメニュー表示・非表示
    $(".cart").click(function(event) {
        $(".cart_menu").toggle();
        $(".language_menu,.accountNavi").hide();
        event.stopPropagation();
        return false;
    });

    $(".cart_delBtn:not('.cart_delAllBtn')").click(function(event){
        event.preventDefault();
        event.stopPropagation();
        deleteCartRow.call(this);
    });

    $(".cart_delAllBtn").click(function(event){
        event.preventDefault();
        event.stopPropagation();
        deleteCartRowAll.call(this);
    });

    $(".cart_menu").on("click", ".cart_delBtn", function(event){
        event.stopPropagation();
        event.preventDefault();
        deleteCartRow.call(this);
    });

    $(".cart_menu").click(function(event) {
        event.stopPropagation();
    });

    $(".cart_btn").click(function(event) {
        event.stopPropagation();
    });

    // カート内にギフト券があれば表示を変更する
    if (parseInt($(".cart_count").text()) > 0) {
        $(".cart_count").show();
        $(".cart_empty").hide();
        $(".cart_btn").show();
        $(".cart_header").show();
    }

    // アカウントメニュー表示・非表示
    $(".accountNavi_trigger").click(function(event) {
        $(".cart_menu,.language_menu").hide();
        $(".accountNavi").toggle();
        event.stopPropagation();
        return false;
    });

    // 各メニュー要素クリック時の非表示処理を停止
    $(".language_item,.accountNavi_item,.cart_item").click(function(event) {
        event.stopPropagation();
    });

    // メニュー以外の要素クリック時にメニューを閉じる
    $(document).click(function() {
        $(".cart_menu,.language_menu,.accountNavi").hide();
    });

    // jquery-ui selectmenu
    if ($(".selectmenu")[0]) {
        $(".selectmenu").selectmenu();
    }

    // ページネーションリンク
    $(".pagination_link").bind("mouseover", function() {
        // iPadじゃない場合
        if ((userAgent.indexOf('iPad') <= -1) && !is_tablet) {
            $(this).addClass("pagination_link-hover");
        }
    }).bind("mouseout", function() {
        $(this).removeClass("pagination_link-hover");
    });

    $(".pagination_link:not(.pagination_link-disabled)").bind("click", function() {
        location.href = $(this).attr("href");
    });

    //[SP]言語メニュー表示・非表示
    $(".language_item-selected").click(function(event){
        $(this).toggleClass("language_item-open");
        $(".spNavi_languageMenu").slideToggle("fast");
        event.stopPropagation();
    });

    // ページネーションの自由入力対応
    $('.pagination_input').keypress(function (e) {
        // enter
        if (e.which == 13) {
            var page = $(this).val();
            var url = $(this).data('url').split('?');

            changePage(page, url);
        }
    });

    // セレクトメニューで変更時にイベントを起こすものの設定
    if ($(".selectmenu-change")[0]) {
        $(".selectmenu-change").selectmenu({
            change: function(event, ui) {
                eval("" + $(this).attr('change-function') + "('" + $(this).val() + "')");
            }
        });
    }
});

// アンカーリンク用 ヘッダー固定によるずれ補正
$(window).on('load', function() {
    var url = $(location).attr('href');

    if (url.indexOf("#") != -1) {
        var anchor = url.split("#");
        var target = $('#' + decodeURI(anchor[anchor.length - 1]));
        var width = $(window).width();

        if (target.length) {
            if (width > 560) {
                var position = Math.floor(target.offset().top) - 120;
            } else {
                var position = Math.floor(target.offset().top) - 60;
            }

            $("html, body").animate({scrollTop: position}, 0);
        }
    }
});

function changePage(page, url) {
    var new_query;

    if (url[1]) {
        var queries = url[1].split('&');
        new_query = [];

        queries.forEach(function(query) {
            parse_query = query.split('=');

            if (parse_query[0] != 'page') {
                new_query.push(query);
            }
        });

        new_query.push('page=' + page);
        new_query = new_query.join('&');
    } else {
        new_query = 'page=' + page;
    }

    window.location.href = url[0] + '?' + new_query;
}

function deleteCartRow() {
    var thisButton = $(this);
    var gift_id = thisButton.data('id');
    var carrier_id = $('input[name="carrier_id"]:checked').val();

    var deleteCartUrl = "/" + language_code + "/order/delete/";
    var formData = {
        id: gift_id,
        carrier_id: carrier_id
    };

    $(this).addClass("btn-disable");

    Ajax.post(formData, deleteCartUrl)
    .done(function (data) {
        deleteRow(gift_id, data);

        return false;
    })
    .fail(function (data) {
        if (data.status === 401) {
            location.href = "/" + language_code + "/signin/";
        } else {
            if (data.status === 422) {
                var errors = '';

                for (var error in data.responseJSON) {
                    errors += data.responseJSON[error] + '\n';
                }
            } else {
                var errors = common_error_message;
            }

            thisButton.removeClass("btn-disable");
            //エラーメッセージを画面に出力する
            alert(errors);
        }
    });
}

function deleteCartRowAll() {
    var thisButton = $(this);
    var deleteCartUrl = "/" + language_code + "/order/delete/";

    $(this).addClass("btn-disable");
    $(".cart_delBtn").addClass("btn-disable");

    var giftIds = [];
    $(".cart_item").each(function() {
        var gift_id = $(this).find(".cart_delBtn").data('id');
        giftIds.push(gift_id);
    });
    var formData = {id: giftIds};

    Ajax.post(formData, deleteCartUrl)
    .done(function (data) {
        deleteRowAll();
    })
    .fail(function (data) {
        if (data.status === 401) {
            location.href = "/" + language_code + "/signin/";
        } else {
            if (data.status === 422) {
                var errors = '';

                for (var error in data.responseJSON) {
                    errors += data.responseJSON[error] + '\n';
                }
            } else {
                var errors = common_error_message;
            }

            thisButton.removeClass("btn-disable");
            $(".cart_delBtn").removeClass("btn-disable");
            //エラーメッセージを画面に出力する
            alert(errors);
        }
    });

}

function deleteRow(gift_id, data) {
    //Orderページの場合、ロードする
    if (location.pathname.indexOf('/order/') !== -1) {
        location.reload();
    } else {
        // カート内が０件になった場合
        if (data.gift_count === 0) {
            $(".cart_count").text('0').hide();
            $(".cart_empty").show();
            $(".cart_btn").hide();
            $(".cart_header").hide();

            //購入画面の場合
            if (location.pathname.indexOf('/order/') !== -1) {
                location.reload();

                return;
            }
        } else {
            $(".cart_count").text(data.gift_count);//上部カート内ギフト数変更
            $(".order_giftCount").text(data.gift_count);//購入ページギフト数変更
        }

        //上部カート内該当行を削除
        var id_in_cartblock = 'cart_item' + gift_id;

        $("." + id_in_cartblock).remove();

        //商品購入ページの場合、該当商品のボタンを購入可にする
        $("#giftAdd_" + gift_id).removeClass("btn-disable").removeClass("hide");
        $("#giftRemove_" + gift_id).addClass("hide");

        //Orderページの場合、該当商品の行を削除する
        var id_in_order = 'order_' + gift_id;

        $("." + id_in_order).remove();

        // Orderページの場合、合計行を再計算する
        $(".orderTotal").text(data.order_total);
        $(".depositAfterOrder").text(data.deposit_after_order);
    }
}

function deleteRowAll() {
    //Orderページの場合、リロードする
    if (location.pathname.indexOf('/order/') !== -1) {
        location.reload();
    } else {
        $(".cart_count").text('0').hide();
        $(".cart_empty").show();
        $(".cart_btn").hide();
        $(".cart_header").hide();
        $(".cart_item").remove();

        //商品購入ページの場合、該当商品のボタンを購入可にする
        $(".giftList_btn-add").removeClass("btn-disable").removeClass("hide");
        $(".giftList_btn-remove").addClass("hide");
    }
}

//表示順
function sort(url) {
    window.location.href = url;
}

//表示件数
function page_count(url) {
    window.location.href = url;
}

// 通知バー表示
function viewNotification(param) {
    // 表示メッセージを設定
    var $notification_box = $(
        '<div class="notification_box notification_box-' + param.type + '">' +
        '<span class="notification_message">' + param.message + '</span>' +
        '</div>'
    );

    $('body').append($notification_box);

    // 通知を表示
    $notification_box
        .slideDown(500)
        .delay(5000)
        .slideUp(500, function () {
            // 表示後、作成された要素を削除
            $notification_box.remove();
        });
}