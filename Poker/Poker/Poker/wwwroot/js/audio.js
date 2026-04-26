window.playAudio = (file) => {
    const key = file.replace(/^\/+/, '');
    const cachedAudio = window.assetCache && window.assetCache.audio[key];

    if (cachedAudio) {
        cachedAudio.currentTime = 0;
        cachedAudio.play().catch(err => {
            console.warn(`Autoplay blocked for ${key}. User must click the page first.`);
        });
    } else {
        console.warn(`Sound ${file} (normalized: ${key}) not found in cache. Loading on-demand.`);
        const audio = new Audio(file);
        audio.play().catch(() => { });
    }
};