document.querySelector('.sidebar-toggle')?.addEventListener('click',()=>document.querySelector('aside')?.classList.toggle('open'));

const previewUrls=new Map();
const clearPreviewUrls=input=>{(previewUrls.get(input)||[]).forEach(URL.revokeObjectURL);previewUrls.set(input,[])};
document.querySelectorAll('input[type="file"][data-image-preview]').forEach(input=>input.addEventListener('change',()=>{
    const target=document.getElementById(input.dataset.imagePreview);
    if(!target||!input.files?.length)return;
    clearPreviewUrls(input);
    const url=URL.createObjectURL(input.files[0]);previewUrls.set(input,[url]);
    target.replaceChildren(Object.assign(document.createElement('img'),{src:url,alt:'Ảnh đã chọn'}),Object.assign(document.createElement('span'),{textContent:`Ảnh mới: ${input.files[0].name}`}));
}));
document.querySelectorAll('input[type="file"][data-images-preview]').forEach(input=>input.addEventListener('change',()=>{
    const target=document.getElementById(input.dataset.imagesPreview);
    if(!target)return;
    clearPreviewUrls(input);target.replaceChildren();
    [...(input.files||[])].forEach((file,index)=>{
        const url=URL.createObjectURL(file);previewUrls.get(input).push(url);
        const article=document.createElement('article');
        article.append(Object.assign(document.createElement('img'),{src:url,alt:''}),Object.assign(document.createElement('span'),{textContent:`Ảnh mới ${index+1}: ${file.name}`}));
        target.append(article);
    });
}));
window.addEventListener('pagehide',()=>previewUrls.forEach((_,input)=>clearPreviewUrls(input)));

document.querySelectorAll('[data-html-editor]').forEach(editor=>{
    const content=editor.querySelector('.html-editor-content');
    const value=editor.querySelector('[data-html-editor-value]');
    editor.querySelectorAll('.html-editor-toolbar button').forEach(button=>{
        button.addEventListener('mousedown',event=>event.preventDefault());
        button.addEventListener('click',()=>{
            content.focus();
            if(button.dataset.link!==undefined){
                const url=window.prompt('Nhập URL liên kết (https://, mailto: hoặc tel:):');
                if(url)document.execCommand('createLink',false,url);
            }else if(button.dataset.block){
                document.execCommand('formatBlock',false,button.dataset.block);
            }else{
                document.execCommand(button.dataset.command,false);
            }
        });
    });
    editor.closest('form')?.addEventListener('submit',()=>value.value=content.innerHTML);
});

document.addEventListener('click',event=>document.querySelectorAll('.user-menu[open]').forEach(menu=>{
    if(!menu.contains(event.target))menu.removeAttribute('open');
}));
document.addEventListener('keydown',event=>{
    if(event.key==='Escape')document.querySelectorAll('.user-menu[open]').forEach(menu=>menu.removeAttribute('open'));
});
