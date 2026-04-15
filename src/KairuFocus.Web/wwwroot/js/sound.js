window.kairufocusSound = {
    play: function (soundFile) {
        if (!soundFile) return;
        var audio = new Audio(soundFile);
        audio.play().catch(function () { /* autoplay may be blocked */ });
    }
};
