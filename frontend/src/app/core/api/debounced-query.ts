import { Observable, OperatorFunction, switchMap, timer } from 'rxjs';

// The outer switch cancels the previous HTTP request as soon as input changes.
export function debouncedQuery<T, R>(project: (value: T) => Observable<R>): OperatorFunction<T, R> {
  return switchMap((value: T) => timer(300).pipe(switchMap(() => project(value))));
}
