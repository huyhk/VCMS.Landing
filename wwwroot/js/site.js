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

const prepareHeroBackgrounds=()=>document.querySelectorAll('.hero-backgrounds img[data-src]').forEach(image=>{
    image.src=image.dataset.src;
    image.removeAttribute('data-src');
});
setTimeout(prepareHeroBackgrounds,6000);

if(!window.matchMedia('(prefers-reduced-motion: reduce)').matches){document.querySelectorAll('.hero-backgrounds').forEach(container=>{const images=[...container.querySelectorAll('img')];if(images.length<2)return;let index=0;setInterval(()=>{if(document.hidden)return;const next=(index+1)%images.length;if(!images[next].src)return;images[index].classList.remove('active');index=next;images[index].classList.add('active')},10000)})}

const galleryItems=[...document.querySelectorAll('[data-gallery-item]')];
if(galleryItems.length){
    const lightbox=document.createElement('div');
    lightbox.className='gallery-lightbox';
    lightbox.setAttribute('role','dialog');
    lightbox.setAttribute('aria-modal','true');
    lightbox.setAttribute('aria-label','Xem hình ảnh');
    lightbox.innerHTML='<button type="button" class="gallery-lightbox-close" aria-label="Đóng">×</button><button type="button" class="gallery-lightbox-nav previous" aria-label="Ảnh trước">‹</button><figure><img alt=""><figcaption></figcaption></figure><button type="button" class="gallery-lightbox-nav next" aria-label="Ảnh tiếp theo">›</button>';
    document.body.append(lightbox);

    const lightboxImage=lightbox.querySelector('img');
    const caption=lightbox.querySelector('figcaption');
    const previousButton=lightbox.querySelector('.previous');
    const nextButton=lightbox.querySelector('.next');
    let activeItems=[];
    let activeIndex=0;
    let returnFocus=null;

    const showImage=index=>{
        if(!activeItems.length)return;
        activeIndex=(index+activeItems.length)%activeItems.length;
        const item=activeItems[activeIndex];
        const thumbnail=item.querySelector('img');
        lightboxImage.src=item.href;
        lightboxImage.alt=thumbnail?.alt??'';
        caption.textContent=thumbnail?.alt??'';
        const showNavigation=activeItems.length>1;
        previousButton.hidden=!showNavigation;
        nextButton.hidden=!showNavigation;
    };
    const closeLightbox=()=>{
        lightbox.classList.remove('open');
        document.body.classList.remove('lightbox-open');
        lightboxImage.removeAttribute('src');
        returnFocus?.focus();
    };
    const openLightbox=item=>{
        const group=item.dataset.galleryGroup;
        activeItems=galleryItems.filter(candidate=>candidate.dataset.galleryGroup===group);
        returnFocus=item;
        showImage(activeItems.indexOf(item));
        lightbox.classList.add('open');
        document.body.classList.add('lightbox-open');
        lightbox.querySelector('.gallery-lightbox-close').focus();
    };

    galleryItems.forEach(item=>item.addEventListener('click',event=>{event.preventDefault();openLightbox(item)}));
    lightbox.querySelector('.gallery-lightbox-close').addEventListener('click',closeLightbox);
    previousButton.addEventListener('click',()=>showImage(activeIndex-1));
    nextButton.addEventListener('click',()=>showImage(activeIndex+1));
    lightbox.addEventListener('click',event=>{if(event.target===lightbox)closeLightbox()});
    document.addEventListener('keydown',event=>{
        if(!lightbox.classList.contains('open'))return;
        if(event.key==='Escape')closeLightbox();
        if(event.key==='ArrowLeft')showImage(activeIndex-1);
        if(event.key==='ArrowRight')showImage(activeIndex+1);
    });
}

document.querySelectorAll('[data-media-player]').forEach(player=>player.addEventListener('click',()=>{
    const embedUrl=player.dataset.embedUrl;
    if(!embedUrl)return;
    const iframe=document.createElement('iframe');
    iframe.src=`${embedUrl}${embedUrl.includes('?')?'&':'?'}autoplay=1`;
    iframe.title=player.getAttribute('aria-label')?.replace('Phát video: ','')||'Video';
    iframe.allow='autoplay; encrypted-media; picture-in-picture; fullscreen';
    iframe.allowFullscreen=true;
    iframe.referrerPolicy='strict-origin-when-cross-origin';
    player.replaceWith(iframe);
}));
