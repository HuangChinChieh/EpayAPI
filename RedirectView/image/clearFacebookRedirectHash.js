// Facebookログイン時リダイレクトの「#_=_」を取り除く http://stackoverflow.com/questions/7131909/facebook-callback-appends-to-return-url

if (window.location.hash == '#_=_') {
    history.replaceState
        ? history.replaceState(null, null, window.location.href.split('#')[0])
        : window.location.hash = '';
}
