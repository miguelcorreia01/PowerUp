import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { Subscription } from '../../membership/services/membership.service';

export interface CreateSubscriptionRequest {
  type: 'Monthly' | 'Semestral' | 'Yearly';
  totalPrice: number;
}

export interface UpdateSubscriptionRequest {
  id: string;
  type: 'Monthly' | 'Semestral' | 'Yearly';
  totalPrice: number;
}

// Backend response interface
interface BackendSubscription {
  id: string;
  type: string;
  totalPrice: number;
  isDeleted: boolean;
  deletedAt?: string;
}

@Injectable({ providedIn: 'root' })
export class AdminMembershipService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5255/api';

  // Map enum value to string
  private mapSubscriptionType(
    type: string
  ): 'Monthly' | 'Semestral' | 'Yearly' {
    const normalized = type.trim().toLowerCase();

    switch (normalized) {
      case 'monthly':
        return 'Monthly';
      case 'semestral':
        return 'Semestral';
      case 'yearly':
        return 'Yearly';
      default:
        console.warn(`Unknown subscription type: ${type}, defaulting to Monthly`);
        return 'Monthly';
    }
  }

  // Map backend subscription to frontend subscription
  private mapSubscription(sub: BackendSubscription): Subscription {
    const mapped = {
      id: sub.id,
      type: this.mapSubscriptionType(sub.type),
      totalPrice: sub.totalPrice,
      isDeleted: sub.isDeleted,
      deletedAt: sub.deletedAt
    };
    
    console.log('Mapping subscription:', { 
      original: sub.type, 
      mapped: mapped.type,
      fullSub: sub 
    });
    return mapped;
  }

  // Get all subscriptions
  getAllSubscriptions(): Observable<Subscription[]> {
    return this.http.get<BackendSubscription[]>(`${this.baseUrl}/subscriptions`).pipe(
      tap(subs => console.log('Raw subscriptions from backend:', subs)),
      map(subs => subs.map(sub => this.mapSubscription(sub))),
      tap(mapped => console.log('Mapped subscriptions:', mapped))
    );
  }

  // Create subscription
  createSubscription(data: CreateSubscriptionRequest): Observable<Subscription> {
    const payload = {
      type: data.type,
      totalPrice: data.totalPrice
    };
    
    console.log('Creating subscription with payload:', payload);
    
    return this.http.post<BackendSubscription>(`${this.baseUrl}/subscriptions`, payload).pipe(
      tap(response => console.log('Backend response:', response)),
      map(sub => this.mapSubscription(sub)),
      tap(mapped => console.log('Mapped created subscription:', mapped))
    );
  }

  // Update subscription
  updateSubscription(id: string, data: UpdateSubscriptionRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/subscriptions/${id}`, {
      Id: id,
      Type: data.type,
      TotalPrice: data.totalPrice,
      IsDeleted: false
    });
  }

  // Delete subscription
  deleteSubscription(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/subscriptions/${id}`);
  }
}