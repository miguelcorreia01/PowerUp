import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith, catchError, of, BehaviorSubject, switchMap } from 'rxjs';
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

  // Refresh trigger for subscriptions
  private refreshSubscriptionsTrigger = new BehaviorSubject<void>(undefined);
  
  // Refresh trigger for my subscription
  private refreshMySubscriptionTrigger = new BehaviorSubject<void>(undefined);

  // Available subscriptions
  subscriptions = toSignal(
    this.refreshSubscriptionsTrigger.pipe(
      switchMap(() => this.api.getAvailableSubscriptions().pipe(
        startWith([]),
        catchError(error => {
          console.error('Error loading subscriptions in component:', error);
          return of([]);
        })
      ))
    ),
    { initialValue: [] as Subscription[] }
  );

  activeSubscriptions = computed(() => {
    const subs = this.subscriptions();
    const active = subs.filter(sub => !sub.isDeleted);
    console.log('Active subscriptions computed:', active);
    return active;
  });

  // Current subscription
  mySubscription = toSignal(
    this.refreshMySubscriptionTrigger.pipe(
      switchMap(() => this.api.getMySubscription().pipe(
        startWith(null),
        catchError(error => {
          console.error('Error loading my subscription:', error);
          return of(null);
        })
      ))
    ),
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

  // Refresh methods
  private refreshSubscriptions(): void {
    this.refreshSubscriptionsTrigger.next();
  }

  private refreshMySubscription(): void {
    this.refreshMySubscriptionTrigger.next();
  }

  // Subscribe to a plan
 async subscribe(subscriptionId: string) {
    const currentSub = this.mySubscription();
    if (currentSub?.isActive) {
      this.feedback.set('You already have an active subscription. Please cancel it first.');
      setTimeout(() => this.feedback.set(null), 3000);
      return;
    }

    try {
      this.isLoading.set(true);
      this.feedback.set(null);
      console.log('Subscribing to subscription ID:', subscriptionId);
      
      const result = await this.api.subscribe(subscriptionId).toPromise();
      console.log('Subscribe result:', result);
      
      this.feedback.set('Subscribed successfully!');
      
      setTimeout(() => {
        this.refreshMySubscription();
      }, 500);
      
      setTimeout(() => {
        this.feedback.set(null);
      }, 3000);
    } catch (error: any) {
      console.error('Subscribe error details:', error);
      console.error('Error status:', error?.status);
      console.error('Error error:', error?.error);
      
      let msg = 'Failed to subscribe. Please try again.';
      
      if (error?.error) {
        if (typeof error.error === 'string') {
          msg = error.error;
        } else if (error.error.message) {
          msg = error.error.message;
        } else if (error.error.title) {
          msg = error.error.title;
        } else if (error.error.errors) {
          const validationErrors = Object.values(error.error.errors).flat() as string[];
          msg = validationErrors.join(', ');
        }
      } else if (error?.message) {
        msg = error.message;
      }
      
      this.feedback.set(msg);
      setTimeout(() => this.feedback.set(null), 5000);
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
      
      // Refresh my subscription after cancellation
      this.refreshMySubscription();
      
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
}