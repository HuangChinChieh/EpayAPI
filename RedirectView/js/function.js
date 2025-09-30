$(function(){
    //売れ筋のギフト券
    var $list = $(".popularCategory_list"),
        $clone = $list.clone(true);

    $list.addClass("sp-hide").after($clone);

    $clone.slick({
        slidesToShow: 2,
        slidestoScroll: 2,
        infinite:true,
        autoplay:false,
        dots:true,
    }).addClass("sp-show");

    $('.bxslider').bxSlider({
        auto: true,
        pause: 5000,
        mode: 'vertical',
        pager: false,
        controls: false,
        touchEnabled: false
    });
});
