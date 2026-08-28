const menuButton=document.querySelector('.menu-button');
const siteNavigation=document.getElementById('site-navigation');
const closeMenu=()=>{
    if(!menuButton||!siteNavigation)return;
    siteNavigation.classList.remove('open');
    menuButton.setAttribute('aria-expanded','false');
    menuButton.setAttribute('aria-label','Mở menu');
    menuButton.textContent='☰';
};
menuButton?.addEventListener('click',event=>{
    event.stopPropagation();
    const isOpen=siteNavigation?.classList.toggle('open')??false;
    menuButton.setAttribute('aria-expanded',String(isOpen));
    menuButton.setAttribute('aria-label',isOpen?'Đóng menu':'Mở menu');
    menuButton.textContent=isOpen?'×':'☰';
});
siteNavigation?.querySelectorAll('a').forEach(link=>link.addEventListener('click',closeMenu));
document.addEventListener('click',event=>{
    if(siteNavigation?.classList.contains('open')&&!event.target.closest('.nav'))closeMenu();
});
document.addEventListener('keydown',event=>{if(event.key==='Escape')closeMenu()});
window.addEventListener('resize',()=>{if(window.innerWidth>800)closeMenu()});

if(!window.matchMedia('(prefers-reduced-motion: reduce)').matches){document.querySelectorAll('.hero-backgrounds').forEach(container=>{const images=[...container.querySelectorAll('img')];if(images.length<2)return;let index=0;setInterval(()=>{if(document.hidden)return;images[index].classList.remove('active');index=(index+1)%images.length;images[index].classList.add('active')},10000)})}
