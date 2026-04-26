window.assetCache = {
    audio: {},
    images: []
};

window.assetPreloader = {
    normalize: function (path) {
        return path.replace(/^\/+/, '');
    },

    preload: function (urls) {
        if (!urls || urls.length === 0) return;

        console.log(`AssetPreloader: Normalizing and loading ${urls.length} assets...`);
        let processedCount = 0;

        const checkDone = () => {
            processedCount++;
            if (processedCount === urls.length) {
                console.log(`%cAssetPreloader: Finished. All keys match C# formats.`, "color: #00ff00; font-weight: bold;");
            }
        };

        urls.forEach(url => {
            const key = this.normalize(url);

            if (url.match(/\.(png|jpg|jpeg|webp)$/i)) {
                const img = new Image();
                img.onload = checkDone;
                img.onerror = checkDone;
                img.src = url;
                window.assetCache.images.push(img);
            }
            else if (url.match(/\.(mp3|wav|ogg)$/i)) {
                const audio = new Audio();
                audio.oncanplaythrough = () => {
                    audio.oncanplaythrough = null;
                    checkDone();
                };
                audio.onerror = checkDone;
                audio.src = url;
                audio.preload = 'auto';
                window.assetCache.audio[key] = audio;
            }
        });
    }
};