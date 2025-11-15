import { Component, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith, of, switchMap, firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject } from 'rxjs';
import { HttpClient } from '@angular/common/http';

import { AuthService } from '../../../../core/auth/auth.service';
import { ProfileService } from '../../services/profile.service';
import { Nav } from '../../../../shared/components/nav/nav';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, Nav],
  templateUrl: './profile.html',
  styleUrls: ['./profile.css']
})
export class Profile {
  private readonly auth = inject(AuthService);
  private readonly profileService = inject(ProfileService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  feedback = signal<string | null>(null);

  pendingRoleChange = signal<string | null>(null);
  isUpdatingRole = signal(false);
  

  private refreshTrigger = new BehaviorSubject<void>(undefined);
  
  // Get user ID from route params
  viewingUserId = toSignal(
    this.route.paramMap.pipe(
      switchMap(params => of(params.get('id')))
    ),
    { initialValue: null }
  );

  // Get current logged-in user
  currentUser = toSignal(this.auth.currentUser$, { initialValue: null });
  
  isAdminViewingOtherUser = computed(() => {
    const current = this.currentUser();
    const viewing = this.viewingUserId();
    return current?.role === 'Admin' && viewing && viewing !== current.userId;
  });
  

  user = toSignal(
    this.refreshTrigger.pipe(
      switchMap(() =>
        this.route.paramMap.pipe(
          switchMap(params => {
            const id = params.get('id');
            if (id) {
              return this.profileService.getProfile(id);
            }

            return this.auth.currentUser$.pipe(
              switchMap(user => 
                user?.userId 
                  ? this.profileService.getProfile(user.userId)
                  : of(null)
              )
            );
          }),
          startWith(null)
        )
      )
    ),
    { initialValue: null }
  );


  constructor() {
    effect(() => {
      const currentUser = this.user();
      const pending = this.pendingRoleChange();
      
      if (currentUser && pending) {
        if (currentUser.role === pending) {
          this.pendingRoleChange.set(null);
        }
      } else if (currentUser && !pending) {

      }
    });
  }

  selectedRole = computed(() => {
    const pending = this.pendingRoleChange();
    if (pending !== null) {
      return pending;
    }
    const userRole = this.user()?.role;
    return userRole || '';
  });

  getRoleLabel(role?: string): string {
    return role === 'Instructor' ? 'Instructor'
      : role === 'Admin' ? 'Administrator'
      : 'Member';
  }

async updateRole(): Promise<void> {
  const userId = this.viewingUserId();
  const newRole = this.selectedRole();

  if (!userId || !newRole) return;

  const currentUserData = this.user();
  if (!currentUserData || currentUserData.role === newRole) return;

  this.isUpdatingRole.set(true);
  this.feedback.set(null);

  this.pendingRoleChange.set(newRole);

  try {
    await firstValueFrom(
      this.http.put(`http://localhost:5255/api/users/${userId}/role`, { role: newRole })
    );

    this.feedback.set('Role updated successfully!');

    this.refreshUser();

    setTimeout(() => this.feedback.set(null), 3000);
  } catch (error: any) {
    console.error('Error updating role:', error);
    const errorMessage = error?.error?.message || error?.message || 'Failed to update role. Please try again.';
    this.feedback.set(errorMessage);

    setTimeout(() => this.feedback.set(null), 3000);
  } finally {
    this.isUpdatingRole.set(false);
  }
}

  refreshUser(): void {
    this.refreshTrigger.next();
  }
  onRoleChange(newRole: string): void {
    this.pendingRoleChange.set(newRole);
  }

  getAvailableRoles(): string[] {
    return ['Member', 'Instructor', 'Admin'];
  }
}