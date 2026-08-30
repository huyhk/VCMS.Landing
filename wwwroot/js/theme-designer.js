(() => {
    const form = document.querySelector('#theme-designer');
    const preview = form?.querySelector('.preview-browser');
    if (!form || !preview) return;
    const radius = { sharp: '0px', soft: '12px', rounded: '24px' };
    const buttonRadius = { square: '2px', soft: '8px', pill: '999px' };
    const shadows = { none: 'none', soft: '0 10px 30px rgba(15,23,42,.10)', strong: '0 18px 50px rgba(15,23,42,.22)' };
    const update = () => {
        form.querySelectorAll('[data-theme-token]').forEach(input => {
            preview.style.setProperty(`--demo-${input.dataset.themeToken}`, input.value);
            const output = input.parentElement.querySelector('output');
            if (output) output.textContent = input.value.toUpperCase();
        });
        preview.style.setProperty('--demo-radius', radius[form.querySelector('[data-theme-choice="corners"]').value]);
        preview.style.setProperty('--demo-button-radius', buttonRadius[form.querySelector('[data-theme-choice="buttons"]').value]);
        preview.style.setProperty('--demo-shadow', shadows[form.querySelector('[data-theme-choice="shadow"]').value]);
    };
    form.addEventListener('input', update);
    update();
})();
