import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith, switchMap, BehaviorSubject } from 'rxjs';
import { Nav } from '../../../../shared/components/nav/nav';
import { AdminMembershipService, CreateSubscriptionRequest, UpdateSubscriptionRequest } from '../../services/admin-membership.service';
import { Subscription } from '../../../membership/services/membership.service';

@Component({
  selector: 'app-admin-memberships',
  standalone: true,
  imports: [CommonModule, FormsModule, Nav],
  templateUrl: './admin-membership.html',
  styleUrls: ['./admin-membership.css']
})
export class AdminMembership {
  private api = inject(AdminMembershipService);


  private refreshTrigger = new BehaviorSubject<void>(undefined);

  subscriptions = toSignal(
    this.refreshTrigger.pipe(
      switchMap(() => this.api.getAllSubscriptions().pipe(startWith([])))
    ),
    { initialValue: [] as Subscription[] }
  );

  showCreateModal = signal(false);
  showEditModal = signal(false);
  selectedSubscription = signal<Subscription | null>(null);
  feedback = signal<string | null>(null);
  isLoading = signal(false);


  formData = signal<CreateSubscriptionRequest>({
    type: 'Monthly',
    totalPrice: 0
  });

  showDeleted = signal(false);
  
  filteredSubscriptions = computed(() => {
    const subs = this.subscriptions();
    const showDeleted = this.showDeleted();
    return showDeleted ? subs : subs.filter(s => !s.isDeleted);
  });

  getTypeDisplay(type: string): string {
    return type === 'Monthly' ? 'Monthly Plan' 
         : type === 'Semestral' ? '6-Month Plan' 
         : 'Yearly Plan';
  }


  formatPrice(price: number): string {
    return `$${price.toFixed(2)}`;
  }


  private refreshSubscriptions(): void {
    this.refreshTrigger.next();
  }

  // Open create modal
  openCreateModal(): void {
    this.feedback.set(null);
    this.formData.set({ type: 'Monthly', totalPrice: 0 });
    this.showCreateModal.set(true);
  }

  // Open edit modal
  openEditModal(subscription: Subscription): void {
    this.feedback.set(null);
    this.selectedSubscription.set(subscription);
    this.formData.set({
      type: subscription.type,
      totalPrice: subscription.totalPrice
    });
    this.showEditModal.set(true);
  }

  // Close modals
  closeModals(): void {
    this.showCreateModal.set(false);
    this.showEditModal.set(false);
    this.selectedSubscription.set(null);
    this.feedback.set(null);
  }

  // Create subscription
  async createSubscription(): Promise<void> {
    const data = this.formData();
    if (data.totalPrice <= 0) {
      this.feedback.set('Price must be greater than 0');
      setTimeout(() => this.feedback.set(null), 3000);
      return;
    }

    try {
      this.isLoading.set(true);
      this.feedback.set(null);
      await this.api.createSubscription(data).toPromise();
      this.feedback.set('Membership created successfully!');
      this.refreshSubscriptions(); // Refresh the list
      setTimeout(() => {
        this.closeModals();
        this.feedback.set(null);
      }, 1500);
    } catch (error: any) {
      console.error('Create subscription error:', error);
      let msg = 'Failed to create membership. Please try again.';
      
      if (error?.error) {
        if (error.error.errors) {
          const validationErrors = Object.values(error.error.errors).flat() as string[];
          msg = validationErrors.join(', ');
        } else if (error.error.message) {
          msg = error.error.message;
        } else if (error.error.title) {
          msg = error.error.title;
        } else if (typeof error.error === 'string') {
          msg = error.error;
        }
      } else if (error?.message) {
        msg = error.message;
      }
      
      this.feedback.set(msg);
    } finally {
      this.isLoading.set(false);
    }
  }

  // Update subscription
  async updateSubscription(): Promise<void> {
    const sub = this.selectedSubscription();
    if (!sub) return;

    const data = this.formData();
    if (data.totalPrice <= 0) {
      this.feedback.set('Price must be greater than 0');
      setTimeout(() => this.feedback.set(null), 3000);
      return;
    }

    try {
      this.isLoading.set(true);
      this.feedback.set(null);
      await this.api.updateSubscription(sub.id, {
        id: sub.id,
        type: data.type,
        totalPrice: data.totalPrice
      }).toPromise();
      this.feedback.set('Membership updated successfully!');
      this.refreshSubscriptions(); 
      setTimeout(() => {
        this.closeModals();
        this.feedback.set(null);
      }, 1500);
    } catch (error: any) {
      console.error('Update subscription error:', error);
      let msg = 'Failed to update membership. Please try again.';
      
      if (error?.error) {
        if (error.error.errors) {
          const validationErrors = Object.values(error.error.errors).flat() as string[];
          msg = validationErrors.join(', ');
        } else if (error.error.message) {
          msg = error.error.message;
        }
      }
      
      this.feedback.set(msg);
    } finally {
      this.isLoading.set(false);
    }
  }

  // Delete subscription
  async deleteSubscription(subscription: Subscription): Promise<void> {
    if (!confirm(`Are you sure you want to delete the ${this.getTypeDisplay(subscription.type)}?`)) {
      return;
    }

    try {
      this.isLoading.set(true);
      this.feedback.set(null);
      await this.api.deleteSubscription(subscription.id).toPromise();
      this.feedback.set('Membership deleted successfully!');
      this.refreshSubscriptions();
      setTimeout(() => this.feedback.set(null), 3000);
    } catch (error: any) {
      console.error('Delete subscription error:', error);
      const msg = error?.error?.message || 'Failed to delete membership. Please try again.';
      this.feedback.set(msg);
      setTimeout(() => this.feedback.set(null), 3000);
    } finally {
      this.isLoading.set(false);
    }
  }
}