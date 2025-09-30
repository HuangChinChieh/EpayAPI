var Ajax = {
    post: function(formData, url) {
        var defer = $.Deferred();
        $.ajax({
            type: 'POST',
            url: url,
            data: formData,
            dataType: 'json',
            headers: { 'X-CSRF-Token' : $('meta[name=_token]').attr('content') },
            success: defer.resolve,
            error: defer.reject
        });
        return defer.promise();
    },

    get: function(formData, url) {
        var defer = $.Deferred();
        $.ajax({
            type: 'GET',
            url: url,
            data: formData,
            dataType: 'json',
            headers: { 'X-CSRF-Token' : $('meta[name=_token]').attr('content') },
            success: defer.resolve,
            error: defer.reject
        });
        return defer.promise();
    },
    
    getNotCsrf: function(formData, url) {
        var defer = $.Deferred();
        $.ajax({
            type: 'GET',
            url: url,
            data: formData,
            dataType: 'json',
            success: defer.resolve,
            error: defer.reject
        });
        return defer.promise();
    },
};

var AjaxFile = {
    post: function(formData, url) {
        var defer = $.Deferred();
        $.ajax({
            type: 'POST',
            url: url,
            data: formData,
            processData: false,
            contentType: false,
            dataType: 'json',
            headers: { 'X-CSRF-Token' : $('meta[name=_token]').attr('content') },
            success: defer.resolve,
            error: defer.reject
        });
        return defer.promise();
    }
};