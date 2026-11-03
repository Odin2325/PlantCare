import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, computed, effect, HostListener, inject, input, signal, untracked, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin, Subscription } from 'rxjs';
import { AuthService } from '../../core/auth/services/auth.service';
import { PhotoDraftState } from '../../core/services/photo-draft-state';
import { UserPlant } from '../../features/my-plants/models/user-plant.model';
import { environment } from '../../../environments/environment';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

export interface JournalEntry { id:string; userPlantId:string; photographedOn:string; notes:string|null; createdAtUtc:string; version:string; }
interface Capabilities { uploadsEnabled:boolean; canUpload:boolean; maximumFileBytes:number; }

@Component({selector:'app-photo-journal',standalone:true,imports:[ReactiveFormsModule,DatePipe],
  templateUrl:'./photo-journal.html',styleUrl:'./photo-journal.css',changeDetection:ChangeDetectionStrategy.OnPush})
export class PhotoJournal {
  readonly plant = input.required<UserPlant>();
  private readonly auth = inject(AuthService); private readonly http = inject(HttpClient);
  private readonly drafts = inject(PhotoDraftState); private readonly destroy = inject(DestroyRef);
  private readonly context = signal(''); private readonly rows = signal<JournalEntry[]>([]);
  private readonly previews = signal<Record<string,string>>({}); private readonly sourcePreview = signal('');
  private readonly large = signal(''); private file:File|null = null; private requestId:string|null = null;
  private subscriptions:Subscription[] = []; private generation = 0;
  private viewerGeneration = 0;
  private readonly viewer = viewChild<ElementRef<HTMLElement>>('viewer');
  private imageTrigger:HTMLElement|null = null;
  private rejectionReason = '';
  readonly current = computed(() => !!this.auth.currentUser() && this.context() === this.key());
  readonly entries = computed(() => this.current() ? this.rows() : []);
  readonly thumbnails = computed(() => this.current() ? this.previews() : {});
  readonly preview = computed(() => this.current() ? this.sourcePreview() : '');
  readonly fullImage = computed(() => this.current() ? this.large() : '');
  readonly loading = signal(false); readonly ready = signal(false); readonly busy = signal(false);
  readonly error = signal(''); readonly message = signal(''); readonly capabilities = signal<Capabilities|null>(null);
  readonly online = signal(navigator.onLine); readonly uncertain = signal(false); readonly retryAllowed = signal(false);
  readonly deleting = signal<string|null>(null); readonly editing = signal<JournalEntry|null>(null); readonly editConflict = signal(false);
  readonly needsRefresh = signal(false); readonly oldestFirst = signal(false); readonly hasFile = signal(false);
  private readonly metadataDraft = signal(false);
  private readonly key = computed(() => `${this.auth.currentUser()?.id ?? ''}/${this.plant().id}`);
  readonly canUpload = computed(() => this.current() && !!this.capabilities()?.canUpload && this.plant().isActive);
  readonly hasDraft = computed(() => this.current() && (this.hasFile() || this.metadataDraft() || !!this.editing()));
  get today():string { const d=new Date(); return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`; }
  readonly form = new FormGroup({date:new FormControl(this.today,{nonNullable:true,validators:[Validators.required]}),
    notes:new FormControl('',{nonNullable:true,validators:[Validators.maxLength(500)]})},
    {validators:control=>control.get('date')?.value>this.today?{future:true}:null});
  readonly editForm = new FormGroup({date:new FormControl('',{nonNullable:true,validators:[Validators.required]}),
    notes:new FormControl('',{nonNullable:true,validators:[Validators.maxLength(500)]})},
    {validators:control=>control.get('date')?.value>this.today?{future:true}:null});
  constructor() {
    effect(onCleanup => {
      const key=this.key();
      untracked(() => { this.reset(); this.context.set(key); if(this.auth.currentUser()) this.load(); });
      onCleanup(() => untracked(() => this.reset()));
    });
    this.destroy.onDestroy(this.drafts.register(() => this.hasDraft()));
    effect(()=>{const viewer=this.viewer();if(viewer&&this.fullImage())untracked(()=>viewer.nativeElement.focus());});
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroy)).subscribe(()=>{this.metadataDraft.set(this.form.dirty);this.drafts.reloadApproved=false;});
    this.editForm.valueChanges.pipe(takeUntilDestroyed(this.destroy)).subscribe(()=>this.drafts.reloadApproved=false);
  }
  private url():string { return `${environment.apiUrl}/api/my-plants/${encodeURIComponent(this.plant().id)}/photos`; }
  private valid(generation:number):boolean { return this.current() && generation===this.generation; }
  private track(subscription:Subscription):void { this.subscriptions.push(subscription); }
  private revoke(url:string):void { if(url) URL.revokeObjectURL(url); }
  private clearImages():void { Object.values(this.previews()).forEach(url=>this.revoke(url)); this.previews.set({}); this.closeImage(); }
  private reset():void {
    this.generation++; this.subscriptions.forEach(sub=>sub.unsubscribe()); this.subscriptions=[];
    this.clearImages(); this.clearDraft(); this.rows.set([]); this.editing.set(null); this.deleting.set(null);
    this.capabilities.set(null); this.ready.set(false); this.busy.set(false); this.loading.set(false);
    this.error.set(''); this.message.set(''); this.needsRefresh.set(false);
  }
  private clearDraft():void {
    this.revoke(this.sourcePreview()); this.sourcePreview.set(''); this.file=null; this.hasFile.set(false);
    this.requestId=null; this.uncertain.set(false); this.retryAllowed.set(false);
    this.rejectionReason='';
    this.form.enable({emitEvent:false}); this.form.reset({date:this.today,notes:''});
  }
  load():void {
    if(!this.current() || this.busy()) return;
    if(!this.online()){this.error.set('Photo journal needs a connection. Changes are not queued.');return;}
    this.generation++;this.subscriptions.forEach(sub=>sub.unsubscribe());this.subscriptions=[];
    const generation=this.generation, url=this.url(); this.loading.set(true); this.ready.set(false); this.error.set('');
    this.clearImages();
    this.track(forkJoin({capabilities:this.http.get<Capabilities>(url+'/capabilities'), entries:this.http.get<JournalEntry[]>(url+'?oldestFirst='+this.oldestFirst())}).subscribe({
      next:result=>{if(!this.valid(generation))return;this.rows.set(result.entries);this.capabilities.set(result.capabilities);
        this.loading.set(false);this.ready.set(true);this.needsRefresh.set(false);
        for(const entry of result.entries.slice(0,20))this.blob(url+'/'+entry.id+'/thumbnail',65536,objectUrl=>this.previews.update(urls=>({...urls,[entry.id]:objectUrl})),generation);},
      error:()=>{if(!this.valid(generation))return;this.loading.set(false);this.error.set('Photo journal could not be loaded. Refresh or sign in again.');},
    }));
  }
  private blob(url:string, maximum:number, accept:(url:string)=>void, generation:number):void {
    this.track(this.http.get(url,{responseType:'blob'}).subscribe({next:blob=>{
      if(!this.valid(generation))return;
      if(blob.type!=='image/jpeg'||blob.size>maximum){this.error.set('This photo could not be displayed.');return;}
      accept(URL.createObjectURL(blob));
    },error:()=>{if(this.valid(generation))this.error.set('A photo could not be loaded. Reconnect and refresh to try again.');}}));
  }
  view(entry:JournalEntry):void {
    if(!this.current()||!this.online())return;this.closeImage();
    this.imageTrigger=document.activeElement instanceof HTMLElement?document.activeElement:null;
    const viewer=this.viewerGeneration;
    this.blob(this.url()+'/'+entry.id+'/image',524288,url=>{if(viewer!==this.viewerGeneration){this.revoke(url);return;}this.large.set(url);this.message.set('Photo opened below.');},this.generation);
  }
  closeImage(restoreFocus=false):void { this.viewerGeneration++;this.revoke(this.large());this.large.set('');
    if(restoreFocus&&this.imageTrigger?.isConnected)this.imageTrigger.focus();
    this.imageTrigger=null;if(this.message()==='Photo opened below.')this.message.set(''); }
  select(event:Event):void {
    if(!this.current()||!this.canUpload()||!this.online()||this.busy()||this.uncertain())return;
    const input=event.target as HTMLInputElement, file=input.files?.[0]; input.value='';
    if(!file)return;
    if(!['image/jpeg','image/png','image/webp'].includes(file.type)||file.size>8388608||file.size===0){this.error.set('Choose a JPEG, PNG or static WebP photo up to 8 MiB. Convert HEIC photos first.');return;}
    if(this.form.pristine)this.form.patchValue({date:this.today},{emitEvent:false});
    this.revoke(this.sourcePreview());this.file=file;this.hasFile.set(true);this.sourcePreview.set(URL.createObjectURL(file));
    this.requestId=crypto.randomUUID();this.error.set('');this.message.set('');this.drafts.reloadApproved=false;
  }
  save():void {
    if(!this.current()||!this.canUpload()||!this.online()||!this.ready()||this.needsRefresh()||this.busy()||!this.file||!this.requestId
      ||this.form.invalid||this.form.getRawValue().date>this.today||(this.uncertain()&&!this.retryAllowed()))return;
    const generation=this.generation, form=this.form.getRawValue(), body=new FormData();
    body.append('requestId',this.requestId);body.append('photographedOn',form.date);body.append('notes',form.notes);body.append('file',this.file,'photo');
    this.busy.set(true);this.error.set('');this.message.set('');this.rejectionReason='';this.form.disable({emitEvent:false});this.retryAllowed.set(false);
    this.track(this.http.post<JournalEntry>(this.url(),body).subscribe({next:()=>{
      if(!this.valid(generation))return;this.busy.set(false);this.clearDraft();this.message.set('Photo saved.');this.load();
    },error:(error:HttpErrorResponse)=>{if(!this.valid(generation))return;this.busy.set(false);this.uncertain.set(true);
      this.rejectionReason=({400:'Check the photo date, notes or sign-in session.',409:'Review the storage limit or plant state.',
        413:'This photo is too large.',415:'Choose JPEG, PNG or static WebP.',422:'This photo could not be validated or resized; choose a smaller supported photo.'} as Record<number,string>)[error.status]??'';
      this.error.set(`${this.rejectionReason} The save could not be confirmed. Check its status before changing this draft.`.trim());}}));
  }
  reconcile():void {
    if(!this.current()||!this.requestId||this.busy()||!this.online())return;
    const generation=this.generation;this.busy.set(true);this.error.set('');this.message.set('');
    this.track(this.http.get<JournalEntry>(this.url()+'/requests/'+this.requestId).subscribe({next:()=>{
      if(!this.valid(generation))return;this.busy.set(false);this.clearDraft();this.message.set('The earlier photo was saved.');this.load();
    },error:(error:HttpErrorResponse)=>{if(!this.valid(generation))return;this.busy.set(false);
      if(error.status===404){this.retryAllowed.set(!this.rejectionReason);this.message.set(this.rejectionReason
        ?`${this.rejectionReason} No saved entry found. Discard this draft to make a change; an earlier request may still finish.`
        :'Not found yet. Retry this unchanged photo or check again; the earlier request may still finish.');}
      else this.error.set('Save status could not be checked. Reconnect or sign in again, then check again.');}}));
  }
  discard():void { if(this.hasDraft()&&!window.confirm('Discard this photo draft? An uncertain upload may still finish.'))return;this.clearDraft();this.editing.set(null);this.error.set(''); }
  beginEdit(entry:JournalEntry):void { if(!this.current()||!this.ready()||this.hasDraft()||this.busy()||this.needsRefresh()||!this.online())return;
    this.editing.set(entry);this.editConflict.set(false);this.editForm.reset({date:entry.photographedOn,notes:entry.notes??''}); }
  cancelEdit():void { if(this.editForm.dirty&&!window.confirm('Discard these unsaved photo notes?'))return;this.editing.set(null);this.editConflict.set(false); }
  saveEdit():void {
    const entry=this.editing(), form=this.editForm.getRawValue();
    if(!this.current()||!entry||this.busy()||!this.online()||this.editConflict()||this.editForm.invalid||form.date>this.today)return;
    const generation=this.generation;this.busy.set(true);this.error.set('');this.message.set('');
    this.track(this.http.patch<JournalEntry>(this.url()+'/'+entry.id,{version:entry.version,photographedOn:form.date,notes:form.notes}).subscribe({
      next:()=>{if(!this.valid(generation))return;this.busy.set(false);this.editing.set(null);this.message.set('Photo notes updated.');this.load();},
      error:()=>{if(!this.valid(generation))return;this.busy.set(false);this.editConflict.set(true);this.error.set('The edit could not be confirmed. Refresh, then cancel and reopen the editor before trying again. Your draft is still here.');},
    }));
  }
  remove(id:string):void {
    if(!this.current()||this.busy()||this.hasDraft()||this.needsRefresh()||!this.online())return;
    const generation=this.generation;this.busy.set(true);this.error.set('');this.message.set('');
    this.track(this.http.delete(this.url()+'/'+id).subscribe({next:()=>{if(!this.valid(generation))return;
      this.busy.set(false);this.deleting.set(null);this.message.set('Photo deleted.');this.load();},
      error:()=>{if(!this.valid(generation))return;this.busy.set(false);this.deleting.set(null);this.needsRefresh.set(true);this.error.set('Deletion could not be confirmed. Refresh before making another change.');}}));
  }
  @HostListener('window:online') connected():void {this.online.set(true);}
  @HostListener('window:offline') disconnected():void {this.online.set(false);}
  @HostListener('window:beforeunload',['$event']) beforeUnload(event:BeforeUnloadEvent):void {
    if(this.hasDraft()&&!this.drafts.reloadApproved){event.preventDefault();event.returnValue='';}
  }
}
