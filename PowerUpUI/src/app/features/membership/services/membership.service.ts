import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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

@Injectable({ providedIn: 'root' })
export class MembershipService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5255/api';

  // Get all available subscriptions
  getAvailableSubscriptions(): Observable<Subscription[]> {
    return this.http.get<Subscription[]>(`${this.baseUrl}/subscriptions`);
  }

  // Get subscription by id
  getSubscription(id: string): Observable<Subscription> {
    return this.http.get<Subscription>(`${this.baseUrl}/subscriptions/${id}`);
  }

  // Get current user's subscription
  getMySubscription(): Observable<UserSubscription> {
    return this.http.get<UserSubscription>(`${this.baseUrl}/subscriptions/my`);
  }

  // Subscribe to a subscription plan
  subscribe(subscriptionId: string): Observable<{ message: string; userSubscription: UserSubscription }> {
    return this.http.post<{ message: string; userSubscription: UserSubscription }>(
      `${this.baseUrl}/usersubscription/subscribe`,
      { subscriptionId }
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
