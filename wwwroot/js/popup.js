(() => {
  const root = document.querySelector('.vcms-popup');
  if (!root) return;
  const id = root.dataset.popupId;
  const version = root.dataset.popupVersion;
  const frequency = root.dataset.popupFrequency || 'session';
  const key = `vcms-popup:${id}:${version}`;
  const today = new Date().toISOString().slice(0, 10);
  const wasShown = () => {
    try {
      if (frequency === 'session') return sessionStorage.getItem(key) === '1';
      if (frequency === 'daily') return localStorage.getItem(key) === today;
      if (frequency === 'once') return localStorage.getItem(key) === '1';
    } catch { }
    return false;
  };
  const remember = () => {
    try {
      if (frequency === 'session') sessionStorage.setItem(key, '1');
      if (frequency === 'daily') localStorage.setItem(key, today);
      if (frequency === 'once') localStorage.setItem(key, '1');
    } catch { }
  };
  if (wasShown()) return;
  const close = () => { root.hidden = true; document.documentElement.classList.remove('vcms-popup-open'); };
  const show = () => {
    root.hidden = false;
    document.documentElement.classList.add('vcms-popup-open');
    remember();
    root.querySelector('.vcms-popup-close, .vcms-popup-cta')?.focus();
  };
  root.querySelectorAll('[data-popup-close]').forEach(element => element.addEventListener('click', close));
  if (root.dataset.popupDismissible === 'true')
    document.addEventListener('keydown', event => { if (event.key === 'Escape') close(); });
  window.setTimeout(show, Math.max(0, Number(root.dataset.popupDelay) || 0) * 1000);
})();
