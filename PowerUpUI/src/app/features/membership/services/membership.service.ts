import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap, catchError, of } from 'rxjs';

export interface Subscription {
  id: string;
  type: 'Monthly' | 'Semestral' | 'Yearly';
  totalPrice: number;
  isDeleted: boolean;
  deletedAt?: string;
}

export interface UserSubscription {
  id: string;
  userId: string;
  subscriptionId: string;
  subscription?: Subscription;
  startDate: string;
  endDate: string;
  isActive: boolean;
  isDeleted: boolean;
}

export interface SubscribeRequest {
  subscriptionId: string;
}

// Backend response interface
interface BackendSubscription {
  id: string;
  type: string | number;
  totalPrice: number;
  isDeleted: boolean;
  deletedAt?: string;
}

@Injectable({ providedIn: 'root' })
export class MembershipService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5255/api';

  // Map enum value to string
  private mapSubscriptionType(
    type: string | number
  ): 'Monthly' | 'Semestral' | 'Yearly' {
    if (typeof type === 'string') {
      const normalized = type.trim().toLowerCase();
      switch (normalized) {
        case 'monthly':
        case '0':
          return 'Monthly';
        case 'semestral':
        case '1':
          return 'Semestral';
        case 'yearly':
        case '2':
          return 'Yearly';
        default:
          console.warn(`Unknown subscription type: ${type}, defaulting to Monthly`);
          return 'Monthly';
      }
    }
    
    if (typeof type === 'number') {
      switch (type) {
        case 0:
          return 'Monthly';
        case 1:
          return 'Semestral';
        case 2:
          return 'Yearly';
        default:
          console.warn(`Unknown subscription type number: ${type}, defaulting to Monthly`);
          return 'Monthly';
      }
    }
    
    console.warn(`Unknown subscription type: ${type} (${typeof type}), defaulting to Monthly`);
    return 'Monthly';
  }

  // Map backend subscription to frontend subscription
  private mapSubscription(sub: BackendSubscription): Subscription {
    return {
      id: sub.id,
      type: this.mapSubscriptionType(sub.type),
      totalPrice: sub.totalPrice,
      isDeleted: sub.isDeleted,
      deletedAt: sub.deletedAt
    };
  }

  // Get all available subscriptions
  getAvailableSubscriptions(): Observable<Subscription[]> {
    return this.http.get<BackendSubscription[]>(`${this.baseUrl}/subscriptions`).pipe(
      tap(subs => console.log('Raw subscriptions from backend:', subs)),
      map(subs => {
        const filtered = subs.filter(sub => !sub.isDeleted);
        console.log('Filtered subscriptions (non-deleted):', filtered);
        return filtered.map(sub => this.mapSubscription(sub));
      }),
      tap(mapped => console.log('Mapped subscriptions:', mapped)),
      catchError(error => {
        console.error('Error fetching subscriptions:', error);
        return of([]);
      })
    );
  }

  // Get subscription by id
  getSubscription(id: string): Observable<Subscription> {
    return this.http.get<BackendSubscription>(`${this.baseUrl}/subscriptions/${id}`).pipe(
      map(sub => this.mapSubscription(sub)),
      catchError(error => {
        console.error('Error fetching subscription:', error);
        throw error;
      })
    );
  }

  // Get current user subscription
  getMySubscription(): Observable<UserSubscription> {
    return this.http.get<UserSubscription>(`${this.baseUrl}/subscriptions/my`).pipe(
      catchError(error => {
        console.error('Error fetching my subscription:', error);
        return of(null as any);
      })
    );
  }

  // Subscribe to a subscription plan
 subscribe(subscriptionId: string): Observable<{ message: string; userSubscription: UserSubscription }> {
    
    const payload = { subscriptionId: subscriptionId };
    console.log('Subscribe payload:', payload);
    
    return this.http.post<{ message: string; userSubscription: UserSubscription }>(
      `${this.baseUrl}/usersubscription/subscribe`,
      payload
    ).pipe(
      tap(response => console.log('Subscribe response:', response)),
      catchError(error => {
        console.error('Subscribe HTTP error:', error);
        throw error;
      })
    );
  }

  // Cancel current subscription
  cancelSubscription(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/usersubscription/cancel`,
      {}
    );
  }
}