(()=>{
const form=document.querySelector('[data-chrome-builder]');if(!form)return;
const componentTypes=['logo','navigation','button','language','phone','email','address','social','company','copyright','text'];
const componentLabels={logo:'Logo',navigation:'Menu chính',button:'Nút hành động',language:'Chọn ngôn ngữ',phone:'Điện thoại',email:'Email',address:'Địa chỉ',social:'Mạng xã hội',company:'Tên/Giới thiệu doanh nghiệp',copyright:'Bản quyền',text:'Văn bản tùy chỉnh'};
const componentHints={logo:'Hình ảnh lấy từ nhóm Nhận diện thương hiệu trong Cấu hình website.',navigation:'Liên kết lấy từ các section đang bật hiển thị trên menu.',language:'Danh sách lấy từ các ngôn ngữ đang được bật.',phone:'Giá trị lấy từ Cấu hình website → Điện thoại.',email:'Giá trị lấy từ Cấu hình website → Email.',address:'Giá trị lấy từ Cấu hình website → Địa chỉ.',social:'Liên kết lấy từ nhóm Mạng xã hội trong Setting values.'};
const choices={background:['surface','brand','contrast','transparent','image'],height:['compact','standard','large'],container:['boxed','full'],width:['auto','fill','1','2','3','4'],align:['left','center','right'],fontWeight:['default','normal','bold'],fontSize:['default','small','large'],textAlign:['default','left','center','right'],spacing:['none','small','medium','large']};
const parse=(value,fallback)=>{try{return JSON.parse(value)}catch{return fallback}};
const languages=(window.chromeBuilderInitial.languages||[]).map(x=>({code:x.code||x.Code,name:x.name||x.Name,isDefault:x.isDefault??x.IsDefault}));
const media=(window.chromeBuilderInitial.media||[]).map(x=>({id:Number(x.id??x.Id),name:x.name||x.Name,url:x.url||x.Url}));
const states={header:parse(window.chromeBuilderInitial.header,{behavior:'sticky',rows:[]}),footer:parse(window.chromeBuilderInitial.footer,{behavior:'static',rows:[]})};
const option=(value,current,label=value)=>{const el=document.createElement('option');el.value=value;el.textContent=label;el.selected=value===current;return el};
const select=(values,current,field,labels={})=>{const el=document.createElement('select');el.dataset.field=field;values.forEach(x=>el.append(option(x,current,labels[x]||x)));return el};
const checkbox=(text,current,field)=>{const label=document.createElement('label');label.className='chrome-checkbox';const el=document.createElement('input');el.type='checkbox';el.checked=!!current;el.dataset.booleanField=field;label.append(el,document.createTextNode(text));return label};
const mediaSelect=(current,field)=>{const el=document.createElement('select');el.dataset.mediaField=field;el.append(option('',String(current||''),'Không dùng ảnh'));media.forEach(x=>el.append(option(String(x.id),String(current||''),x.name)));return el};
const numberField=(labelText,value,min,max,field)=>{const label=document.createElement('label');label.className='chrome-component-field';const caption=document.createElement('span');caption.textContent=labelText;const input=document.createElement('input');input.type='range';input.min=min;input.max=max;input.value=value??0;input.dataset.numberField=field;const output=document.createElement('output');output.textContent=`${input.value}%`;input.addEventListener('input',()=>output.textContent=`${input.value}%`);label.append(caption,input,output);return label};
const uploadField=(labelText,field)=>{const label=document.createElement('label');label.className='chrome-upload-field';const caption=document.createElement('span');caption.textContent=labelText;const input=document.createElement('input');input.type='file';input.accept='image/png,image/jpeg,image/webp';input.dataset.backgroundUpload=field;label.append(caption,input);return label};
const inputField=(labelText,value,placeholder,field,language=null,isDefault=false)=>{const label=document.createElement('label');label.className='chrome-component-field';const caption=document.createElement('span');caption.textContent=labelText;const input=document.createElement('input');input.value=value||'';input.placeholder=placeholder;input.dataset.field=field;if(language){input.dataset.translationLanguage=language;input.dataset.defaultLanguage=String(isDefault)}label.append(caption,input);return label};
const selectField=(labelText,values,current,field,labels={})=>{const label=document.createElement('label');label.className='chrome-component-field';const caption=document.createElement('span');caption.textContent=labelText;label.append(caption,select(values,current,field,labels));return label};
const colorField=(value)=>{const label=document.createElement('label');label.className='chrome-component-field chrome-color-field';const caption=document.createElement('span');caption.textContent='Màu chữ';const controls=document.createElement('span');controls.className='chrome-color-controls';const enabled=document.createElement('input');enabled.type='checkbox';enabled.checked=!!value;enabled.dataset.colorEnabled='textColor';const enabledText=document.createElement('span');enabledText.textContent='Dùng màu riêng';const color=document.createElement('input');color.type='color';color.value=value||'#000000';color.disabled=!value;color.dataset.field='textColor';controls.append(enabled,enabledText,color);label.append(caption,controls);return label};
const button=(text,action,index)=>{const el=document.createElement('button');el.type='button';el.textContent=text;el.className='chrome-icon-button'+(action==='remove'?' danger':'');el.dataset.action=action;if(index!==undefined)el.dataset.index=index;return el};
function render(region){
 const state=states[region],root=form.querySelector(`[data-layout-editor="${region}"]`);root.replaceChildren();
 const behavior=form.querySelector(`[data-behavior="${region}"]`);if(behavior)behavior.value=state.behavior||'static';
 state.rows.forEach((row,ri)=>{
  const rowEl=document.createElement('article');rowEl.className='chrome-builder-row';rowEl.dataset.region=region;rowEl.dataset.row=ri;
  const head=document.createElement('div');head.className='chrome-builder-row-head';
  const key=document.createElement('input');key.value=row.key||`row-${ri+1}`;key.dataset.field='key';key.placeholder='Tên hàng';head.append(key,select(choices.background,row.background,'background'),select(choices.height,row.height,'height'),select(choices.container,row.container,'container'),checkbox('Ẩn mobile',row.hideOnMobile,'hideOnMobile'));
  const actions=document.createElement('div');actions.className='chrome-builder-actions';actions.append(button('↑','row-up'),button('↓','row-down'),button('Xóa','remove-row'));head.append(actions);rowEl.append(head);
  if(row.background==='image'){
   const background=document.createElement('section');background.className='chrome-background-editor';
   const title=document.createElement('strong');title.textContent='Ảnh nền của hàng';background.append(title);
   const desktop=document.createElement('label');desktop.className='chrome-component-field';desktop.innerHTML='<span>Ảnh desktop</span>';desktop.append(mediaSelect(row.backgroundMediaId,'backgroundMediaId'));background.append(desktop,uploadField('Tải ảnh desktop mới','backgroundMediaId'));
   const mobile=document.createElement('label');mobile.className='chrome-component-field';mobile.innerHTML='<span>Ảnh mobile (không bắt buộc)</span>';mobile.append(mediaSelect(row.mobileBackgroundMediaId,'mobileBackgroundMediaId'));background.append(mobile,uploadField('Tải ảnh mobile mới','mobileBackgroundMediaId'));
   const size=document.createElement('label');size.className='chrome-component-field';size.innerHTML='<span>Cách co ảnh</span>';size.append(select(['cover','contain','auto'],row.backgroundSize||'cover','backgroundSize',{cover:'Phủ kín',contain:'Hiển thị toàn bộ',auto:'Kích thước gốc'}));
   const position=document.createElement('label');position.className='chrome-component-field';position.innerHTML='<span>Vị trí ảnh</span>';position.append(select(['center','left','right','top','bottom'],row.backgroundPosition||'center','backgroundPosition',{center:'Giữa',left:'Trái',right:'Phải',top:'Trên',bottom:'Dưới'}));
   const overlay=document.createElement('label');overlay.className='chrome-component-field';overlay.innerHTML='<span>Màu phủ</span>';const color=document.createElement('input');color.type='color';color.value=row.overlayColor||'#000000';color.dataset.field='overlayColor';overlay.append(color);
   background.append(size,position,overlay,numberField('Độ đậm màu phủ',row.overlayOpacity,0,100,'overlayOpacity'));
   const selected=media.find(x=>x.id===Number(row.backgroundMediaId));if(selected){const preview=document.createElement('img');preview.className='chrome-background-preview';preview.src=selected.url;preview.alt='Xem trước ảnh nền';background.append(preview)}
   rowEl.append(background);
  }
  const columns=document.createElement('div');columns.className='chrome-builder-columns';
  (row.columns||=[]).forEach((column,ci)=>{
   const col=document.createElement('section');col.className='chrome-builder-column';col.dataset.column=ci;
   const colHead=document.createElement('div');colHead.className='chrome-builder-column-head';colHead.append(select(choices.width,column.width,'width'),select(choices.align,column.align,'align'),button('×','remove-column'));col.append(colHead);
   (column.components||=[]).forEach((component,pi)=>{
    const item=document.createElement('div');item.className='chrome-builder-component';item.dataset.component=pi;
    const itemHead=document.createElement('div');itemHead.className='chrome-builder-component-head';itemHead.append(select(componentTypes,component.type,'type',componentLabels));
    const controls=document.createElement('div');controls.className='chrome-builder-actions';controls.append(button('↑','component-up'),button('↓','component-down'),button('×','remove-component'));itemHead.append(controls);item.append(itemHead);
    if(componentHints[component.type]){const hint=document.createElement('p');hint.className='chrome-component-hint';hint.textContent=componentHints[component.type];item.append(hint)}
    if(['logo','button'].includes(component.type))item.append(inputField('Biến thể',component.variant,'Ví dụ: primary hoặc light','variant'));
    if(['button','company','copyright','text'].includes(component.type)){
     component.translations||={};
     const activeDefault=languages.find(x=>x.isDefault);if(activeDefault&&component.text&&!component.translations[activeDefault.code])component.translations[activeDefault.code]=component.text;
     languages.forEach(language=>{const value=component.translations[language.code]||(language.isDefault?component.text:'');item.append(inputField(`Nội dung – ${language.name}${language.isDefault?' (mặc định)':''}`,value,'Để trống để dùng nội dung mặc định','translation',language.code,language.isDefault))});
     if(!languages.length)item.append(inputField('Nội dung tùy chỉnh',component.text,'Để trống để dùng nội dung mặc định','text'));
    }
    if(component.type==='button')item.append(inputField('Liên kết',component.url,'Ví dụ: #contact hoặc /bao-gia','url'));
    const formatting=document.createElement('div');formatting.className='chrome-formatting';
    const typography=document.createElement('section');typography.className='chrome-format-group';
    const typographyTitle=document.createElement('strong');typographyTitle.textContent='Kiểu chữ';
    const typographyFields=document.createElement('div');typographyFields.className='chrome-format-grid chrome-format-grid-typography';
    typographyFields.append(
     selectField('Độ đậm',choices.fontWeight,component.fontWeight||'default','fontWeight',{default:'Theo giao diện',normal:'Thường',bold:'Đậm'}),
     selectField('Cỡ chữ',choices.fontSize,component.fontSize||'default','fontSize',{default:'Mặc định',small:'Nhỏ',large:'Lớn'}),
     checkbox('In nghiêng',component.italic,'italic'),colorField(component.textColor));
    typography.append(typographyTitle,typographyFields);
    const alignment=document.createElement('section');alignment.className='chrome-format-group';
    const alignmentTitle=document.createElement('strong');alignmentTitle.textContent='Căn chỉnh & khoảng cách';
    const alignmentFields=document.createElement('div');alignmentFields.className='chrome-format-grid chrome-format-grid-spacing';
    alignmentFields.append(
     selectField('Căn chữ',choices.textAlign,component.textAlign||'default','textAlign',{default:'Theo cột',left:'Trái',center:'Giữa',right:'Phải'}),
     selectField('Khoảng cách trên',choices.spacing,component.spacingTop||'none','spacingTop',{none:'Không',small:'Nhỏ',medium:'Vừa',large:'Lớn'}),
     selectField('Khoảng cách dưới',choices.spacing,component.spacingBottom||'none','spacingBottom',{none:'Không',small:'Nhỏ',medium:'Vừa',large:'Lớn'}));
    alignment.append(alignmentTitle,alignmentFields);formatting.append(typography,alignment);
    item.append(formatting);
    item.append(checkbox('Ẩn thành phần này trên mobile',component.hideOnMobile,'hideOnMobile'));col.append(item);
   });
   const add=button('+ Thành phần','add-component');add.className='button button-secondary';col.append(add);columns.append(col);
  });
  rowEl.append(columns);const addColumn=button('+ Cột','add-column');addColumn.className='button button-secondary';rowEl.append(addColumn);root.append(rowEl);
 });sync(region);
}
function locate(target){const rowEl=target.closest('[data-row]'),colEl=target.closest('[data-column]'),componentEl=target.closest('[data-component]');return{region:rowEl?.dataset.region,row:+rowEl?.dataset.row,column:colEl?+colEl.dataset.column:null,component:componentEl?+componentEl.dataset.component:null}}
function sync(region){form.querySelector(`[data-chrome-json="${region}"]`).value=JSON.stringify(states[region]);}
form.addEventListener('input',event=>{const pos=locate(event.target),field=event.target.dataset.field;if(!pos.region||!field)return;let target=states[pos.region].rows[pos.row];if(pos.column!==null)target=target.columns[pos.column];if(pos.component!==null)target=target.components[pos.component];const language=event.target.dataset.translationLanguage;if(language){target.translations||={};target.translations[language]=event.target.value;if(event.target.dataset.defaultLanguage==='true')target.text=event.target.value}else target[field]=event.target.value;sync(pos.region);if(field==='type'||field==='background')render(pos.region)});
form.addEventListener('change',event=>{
 if(event.target.matches('[data-behavior]')){states[event.target.dataset.behavior].behavior=event.target.value;sync(event.target.dataset.behavior);return}
 const pos=locate(event.target),mediaField=event.target.dataset.mediaField,numberField=event.target.dataset.numberField;
 if(mediaField){states[pos.region].rows[pos.row][mediaField]=event.target.value?Number(event.target.value):null;sync(pos.region);render(pos.region);return}
 if(numberField){states[pos.region].rows[pos.row][numberField]=Number(event.target.value);sync(pos.region);return}
 const colorEnabled=event.target.dataset.colorEnabled;if(colorEnabled){const component=states[pos.region].rows[pos.row].columns[pos.column].components[pos.component];component[colorEnabled]=event.target.checked?'#000000':null;sync(pos.region);render(pos.region);return}
 const field=event.target.dataset.booleanField;if(!field)return;let target=states[pos.region].rows[pos.row];if(pos.column!==null)target=target.columns[pos.column];if(pos.component!==null)target=target.components[pos.component];target[field]=event.target.checked;sync(pos.region);
});
form.addEventListener('change',async event=>{
 const field=event.target.dataset.backgroundUpload;if(!field||!event.target.files?.length)return;
 const pos=locate(event.target),row=states[pos.region].rows[pos.row],data=new FormData(),token=form.querySelector('[name="__RequestVerificationToken"]')?.value;
 data.append('file',event.target.files[0]);if(token)data.append('__RequestVerificationToken',token);event.target.disabled=true;
 try{const response=await fetch(form.dataset.uploadUrl,{method:'POST',body:data});const result=await response.json();if(!response.ok)throw new Error(result.error||'Không thể tải ảnh lên.');const asset={id:Number(result.id),name:result.name,url:result.url};if(!media.some(x=>x.id===asset.id))media.unshift(asset);row[field]=asset.id;sync(pos.region);render(pos.region)}catch(error){event.target.disabled=false;alert(error.message)}
});
form.addEventListener('click',event=>{
 const preset=event.target.closest('[data-preset-region]');if(preset){states[preset.dataset.presetRegion]=parse(preset.dataset.presetJson,{rows:[]});render(preset.dataset.presetRegion);return}
 const addRow=event.target.closest('[data-add-row]');if(addRow){const region=addRow.dataset.addRow;if(states[region].rows.length<5)states[region].rows.push({key:`row-${states[region].rows.length+1}`,container:'boxed',background:region==='footer'?'contrast':'surface',height:'standard',hideOnMobile:false,columns:[{width:'fill',align:'left',components:[]}]});render(region);return}
 const action=event.target.closest('[data-action]');if(!action)return;const p=locate(action),rows=states[p.region].rows,row=rows[p.row],columns=row.columns,column=p.column===null?null:columns[p.column],components=p.component===null||!column?null:column.components;
 switch(action.dataset.action){case'row-up':if(p.row>0)[rows[p.row-1],rows[p.row]]=[rows[p.row],rows[p.row-1]];break;case'row-down':if(p.row<rows.length-1)[rows[p.row+1],rows[p.row]]=[rows[p.row],rows[p.row+1]];break;case'remove-row':rows.splice(p.row,1);break;case'add-column':if(columns.length<4)columns.push({width:'fill',align:'left',components:[]});break;case'remove-column':columns.splice(p.column,1);break;case'add-component':if(column.components.length<10)column.components.push({type:'text',variant:'default',text:'',url:'',fontWeight:'default',italic:false,fontSize:'default',textColor:null,textAlign:'default',spacingTop:'none',spacingBottom:'none',hideOnMobile:false});break;case'remove-component':components.splice(p.component,1);break;case'component-up':if(p.component>0)[components[p.component-1],components[p.component]]=[components[p.component],components[p.component-1]];break;case'component-down':if(p.component<components.length-1)[components[p.component+1],components[p.component]]=[components[p.component],components[p.component+1]];break;}render(p.region);
});
form.addEventListener('submit',()=>{sync('header');sync('footer')});render('header');render('footer');
})();
