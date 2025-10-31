import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith } from 'rxjs';
import { AuthService } from '../../../../core/auth/auth.service';
import { MembershipService, Subscription } from '../../services/membership.service';
import { Nav } from '../../../../shared/components/nav/nav';

@Component({
  selector: 'app-membership',
  standalone: true,
  imports: [CommonModule, RouterLink, Nav],
  templateUrl: './membership.html',
  styleUrls: ['./membership.css']
})
export class Membership {
  private auth = inject(AuthService);
  private api = inject(MembershipService);

  // Available subscriptions
  subscriptions = toSignal(
    this.api.getAvailableSubscriptions().pipe(startWith([])),
    { initialValue: [] as Subscription[] }
  );

  // Current subscription
  mySubscription = toSignal(
    this.api.getMySubscription().pipe(startWith(null)),
    { initialValue: null }
  );

  // Feedback
  feedback = signal<string | null>(null);
  isLoading = signal(false);

  // Check if user has active subscription
  hasActiveSubscription = computed(() => {
    const sub = this.mySubscription();
    return sub?.isActive ?? false;
  });

  // Get subscription type display name
  getTypeDisplay(type: string): string {
    return type === 'Monthly' ? 'Monthly Plan' 
         : type === 'Semestral' ? '6-Month Plan' 
         : 'Yearly Plan';
  }

  // Get subscription duration
  getDuration(type: string): string {
    return type === 'Monthly' ? '1 month' 
         : type === 'Semestral' ? '6 months' 
         : '12 months';
  }

  // Subscribe to a plan
  async subscribe(subscriptionId: string) {
    if (this.hasActiveSubscription()) {
      this.feedback.set('You already have an active subscription. Please cancel it first.');
      setTimeout(() => this.feedback.set(null), 3000);
      return;
    }

    try {
      this.isLoading.set(true);
      await this.api.subscribe(subscriptionId).toPromise();
      this.feedback.set('Subscribed successfully!');
      
      setTimeout(() => this.feedback.set(null), 3000);
    } catch (error: any) {
      const msg = error?.error?.message || 'Failed to subscribe. Please try again.';
      this.feedback.set(msg);
      setTimeout(() => this.feedback.set(null), 3000);
    } finally {
      this.isLoading.set(false);
    }
  }

  // Cancel subscription
async cancelSubscription() {
  if (!this.hasActiveSubscription()) {
    this.feedback.set('No active subscription to cancel.');
    setTimeout(() => this.feedback.set(null), 3000);
    return;
  }

  if (!confirm('Are you sure you want to cancel your subscription? You will lose access when it expires.')) {
    return;
  }

  try {
    this.isLoading.set(true);
    await this.api.cancelSubscription().toPromise();
    this.feedback.set('Subscription cancelled successfully.');
    setTimeout(() => this.feedback.set(null), 3000);
  } catch (error: any) {
    const msg = error?.error?.message || 'Failed to cancel subscription. Please try again.';
    this.feedback.set(msg);
    setTimeout(() => this.feedback.set(null), 3000);
  } finally {
    this.isLoading.set(false);
  }
}

isSubscribedTo(subscriptionId: string): boolean {
  const mySub = this.mySubscription();
  if (!mySub || !mySub.isActive) return false;
  return mySub.subscriptionId.toLowerCase() === subscriptionId.toLowerCase();
}

  // Format price
  formatPrice(price: number): string {
    return `$${price.toFixed(2)}`;
  }

  // Get price per month
  getPricePerMonth(type: string, totalPrice: number): string {
    const months = type === 'Monthly' ? 1 : type === 'Semestral' ? 6 : 12;
    const perMonth = totalPrice / months;
    return `$${perMonth.toFixed(2)}/month`;
  }
}
