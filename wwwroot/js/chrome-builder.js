(()=>{
const form=document.querySelector('[data-chrome-builder]');if(!form)return;
const componentTypes=['logo','navigation','button','language','phone','email','address','social','company','copyright','text'];
const choices={background:['surface','brand','contrast','transparent'],height:['compact','standard','large'],container:['boxed','full'],width:['auto','fill','1','2','3','4'],align:['left','center','right']};
const parse=(value,fallback)=>{try{return JSON.parse(value)}catch{return fallback}};
const states={header:parse(window.chromeBuilderInitial.header,{behavior:'sticky',rows:[]}),footer:parse(window.chromeBuilderInitial.footer,{behavior:'static',rows:[]})};
const option=(value,current)=>{const el=document.createElement('option');el.value=value;el.textContent=value;el.selected=value===current;return el};
const select=(values,current,field)=>{const el=document.createElement('select');el.dataset.field=field;values.forEach(x=>el.append(option(x,current)));return el};
const checkbox=(text,current,field)=>{const label=document.createElement('label');label.className='chrome-checkbox';const el=document.createElement('input');el.type='checkbox';el.checked=!!current;el.dataset.booleanField=field;label.append(el,document.createTextNode(text));return label};
const button=(text,action,index)=>{const el=document.createElement('button');el.type='button';el.textContent=text;el.className='chrome-icon-button'+(action==='remove'?' danger':'');el.dataset.action=action;if(index!==undefined)el.dataset.index=index;return el};
function render(region){
 const state=states[region],root=form.querySelector(`[data-layout-editor="${region}"]`);root.replaceChildren();
 const behavior=form.querySelector(`[data-behavior="${region}"]`);if(behavior)behavior.value=state.behavior||'static';
 state.rows.forEach((row,ri)=>{
  const rowEl=document.createElement('article');rowEl.className='chrome-builder-row';rowEl.dataset.region=region;rowEl.dataset.row=ri;
  const head=document.createElement('div');head.className='chrome-builder-row-head';
  const key=document.createElement('input');key.value=row.key||`row-${ri+1}`;key.dataset.field='key';key.placeholder='Tên hàng';head.append(key,select(choices.background,row.background,'background'),select(choices.height,row.height,'height'),select(choices.container,row.container,'container'),checkbox('Ẩn mobile',row.hideOnMobile,'hideOnMobile'));
  const actions=document.createElement('div');actions.className='chrome-builder-actions';actions.append(button('↑','row-up'),button('↓','row-down'),button('Xóa','remove-row'));head.append(actions);rowEl.append(head);
  const columns=document.createElement('div');columns.className='chrome-builder-columns';
  (row.columns||=[]).forEach((column,ci)=>{
   const col=document.createElement('section');col.className='chrome-builder-column';col.dataset.column=ci;
   const colHead=document.createElement('div');colHead.className='chrome-builder-column-head';colHead.append(select(choices.width,column.width,'width'),select(choices.align,column.align,'align'),button('×','remove-column'));col.append(colHead);
   (column.components||=[]).forEach((component,pi)=>{
    const item=document.createElement('div');item.className='chrome-builder-component';item.dataset.component=pi;
    item.append(select(componentTypes,component.type,'type'));
    const variant=document.createElement('input');variant.value=component.variant||'';variant.placeholder='Biến thể (primary/light)';variant.dataset.field='variant';item.append(variant);
    const controls=document.createElement('div');controls.append(button('↑','component-up'),button('↓','component-down'),button('×','remove-component'));item.append(controls);
    const text=document.createElement('input');text.value=component.text||'';text.placeholder='Nội dung tùy chọn';text.dataset.field='text';
    const url=document.createElement('input');url.value=component.url||'';url.placeholder='URL tùy chọn';url.dataset.field='url';item.append(text,url,checkbox('Ẩn mobile',component.hideOnMobile,'hideOnMobile'));col.append(item);
   });
   const add=button('+ Thành phần','add-component');add.className='button button-secondary';col.append(add);columns.append(col);
  });
  rowEl.append(columns);const addColumn=button('+ Cột','add-column');addColumn.className='button button-secondary';rowEl.append(addColumn);root.append(rowEl);
 });sync(region);
}
function locate(target){const rowEl=target.closest('[data-row]'),colEl=target.closest('[data-column]'),componentEl=target.closest('[data-component]');return{region:rowEl?.dataset.region,row:+rowEl?.dataset.row,column:colEl?+colEl.dataset.column:null,component:componentEl?+componentEl.dataset.component:null}}
function sync(region){form.querySelector(`[data-chrome-json="${region}"]`).value=JSON.stringify(states[region]);}
form.addEventListener('input',event=>{const pos=locate(event.target),field=event.target.dataset.field;if(!pos.region||!field)return;let target=states[pos.region].rows[pos.row];if(pos.column!==null)target=target.columns[pos.column];if(pos.component!==null)target=target.components[pos.component];target[field]=event.target.value;sync(pos.region)});
form.addEventListener('change',event=>{
 if(event.target.matches('[data-behavior]')){states[event.target.dataset.behavior].behavior=event.target.value;sync(event.target.dataset.behavior);return}
 const field=event.target.dataset.booleanField;if(!field)return;const pos=locate(event.target);let target=states[pos.region].rows[pos.row];if(pos.column!==null)target=target.columns[pos.column];if(pos.component!==null)target=target.components[pos.component];target[field]=event.target.checked;sync(pos.region);
});
form.addEventListener('click',event=>{
 const preset=event.target.closest('[data-preset-region]');if(preset){states[preset.dataset.presetRegion]=parse(preset.dataset.presetJson,{rows:[]});render(preset.dataset.presetRegion);return}
 const addRow=event.target.closest('[data-add-row]');if(addRow){const region=addRow.dataset.addRow;if(states[region].rows.length<5)states[region].rows.push({key:`row-${states[region].rows.length+1}`,container:'boxed',background:region==='footer'?'contrast':'surface',height:'standard',hideOnMobile:false,columns:[{width:'fill',align:'left',components:[]}]});render(region);return}
 const action=event.target.closest('[data-action]');if(!action)return;const p=locate(action),rows=states[p.region].rows,row=rows[p.row],columns=row.columns,column=p.column===null?null:columns[p.column],components=p.component===null||!column?null:column.components;
 switch(action.dataset.action){case'row-up':if(p.row>0)[rows[p.row-1],rows[p.row]]=[rows[p.row],rows[p.row-1]];break;case'row-down':if(p.row<rows.length-1)[rows[p.row+1],rows[p.row]]=[rows[p.row],rows[p.row+1]];break;case'remove-row':rows.splice(p.row,1);break;case'add-column':if(columns.length<4)columns.push({width:'fill',align:'left',components:[]});break;case'remove-column':columns.splice(p.column,1);break;case'add-component':if(column.components.length<10)column.components.push({type:'text',variant:'default',text:'',url:'',hideOnMobile:false});break;case'remove-component':components.splice(p.component,1);break;case'component-up':if(p.component>0)[components[p.component-1],components[p.component]]=[components[p.component],components[p.component-1]];break;case'component-down':if(p.component<components.length-1)[components[p.component+1],components[p.component]]=[components[p.component],components[p.component+1]];break;}render(p.region);
});
form.addEventListener('submit',()=>{sync('header');sync('footer')});render('header');render('footer');
})();
