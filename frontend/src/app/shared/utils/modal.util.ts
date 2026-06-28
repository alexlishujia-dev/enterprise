import { firstValueFrom, Observable } from 'rxjs';

/** NG-ZORRO Modal [nzOnOk] 仅支持 Promise，将 Observable 转为 Promise */
export function asModalOk(obs: Observable<boolean>): Promise<boolean> {
  return firstValueFrom(obs);
}

export function modalOk(value: boolean): Promise<boolean> {
  return Promise.resolve(value);
}
