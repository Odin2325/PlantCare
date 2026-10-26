import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { MyPlantsApiService } from '../../features/my-plants/data-access/my-plants-api.service';
import { UserPlant } from '../../features/my-plants/models/user-plant.model';
import { HarvestRecord } from '../../features/my-plants/models/harvest-record.model';
@Component({selector:'app-harvest-log',standalone:true,imports:[ReactiveFormsModule,DatePipe],templateUrl:'./harvest-log.html',styleUrl:'./harvest-log.css',changeDetection:ChangeDetectionStrategy.OnPush})
export class HarvestLog {
  readonly plant=input.required<UserPlant>();
  private readonly api=inject(MyPlantsApiService); private readonly destroyRef=inject(DestroyRef);
  readonly records=signal<HarvestRecord[]>([]); readonly loading=signal(true); readonly ready=signal(false);
  readonly busy=signal(false); readonly error=signal(''); readonly message=signal(''); readonly deleting=signal<string|null>(null);
  readonly today=(()=>{const d=new Date();return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;})();
  readonly form=new FormGroup({
    date:new FormControl(this.today,{nonNullable:true,validators:[Validators.required]}),
    quantity:new FormControl<number|null>(null,[Validators.min(0.001),Validators.max(1000000)]),
    unit:new FormControl('items',{nonNullable:true}),notes:new FormControl('',{nonNullable:true,validators:[Validators.maxLength(1000)]}),
  },{validators:control=>{
    const {date,quantity,unit}=control.value;
    return date>this.today ? {future:true} : quantity!=null && (!/^\d+(\.\d{1,3})?$/.test(String(quantity)) || unit==='items' && !Number.isInteger(quantity)) ? {quantity:true}:null;
  }});
  constructor(){
    effect(onCleanup=>{const sub=this.load();onCleanup(()=>sub.unsubscribe());});
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(()=>this.message.set(''));
  }
  load(){this.loading.set(true);this.ready.set(false);this.error.set('');return this.api.getHarvests(this.plant().id).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.loading.set(false))).subscribe({next:items=>{this.records.set(items);this.ready.set(true);},error:()=>this.error.set('Harvest records could not be loaded. Please try again.')});}
  save():void{
    if(this.busy()||!this.ready()||this.form.invalid||!this.plant().isActive)return;
    const {date,quantity,unit,notes}=this.form.getRawValue();this.busy.set(true);this.error.set('');this.message.set('');this.form.disable({emitEvent:false});
    this.api.recordHarvest(this.plant().id,{harvestedOn:date,quantity,unit:quantity==null?null:unit,notes:notes.trim()||null}).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>{this.busy.set(false);this.form.enable({emitEvent:false});})).subscribe({
      next:item=>{this.records.update(items=>[item,...items].sort((a,b)=>b.harvestedOn.localeCompare(a.harvestedOn)||b.recordedAtUtc.localeCompare(a.recordedAtUtc)).slice(0,100));this.form.reset({date:this.today,quantity:null,unit:'items',notes:''},{emitEvent:false});this.message.set('Harvest recorded.');},
      error:()=>this.error.set('Harvest could not be recorded. Check the date and quantity, or reload if this plant was archived. Your draft is still here.'),
    });
  }
  remove(id:string):void{
    if(this.busy()||this.deleting()!==id||!this.plant().isActive)return;
    this.busy.set(true);this.error.set('');this.message.set('');
    this.api.deleteHarvest(this.plant().id,id).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.busy.set(false))).subscribe({next:()=>{this.deleting.set(null);this.message.set('Harvest removed.');this.load();},error:()=>this.error.set('Harvest could not be removed. The record is still shown; please try again.')});
  }
}
