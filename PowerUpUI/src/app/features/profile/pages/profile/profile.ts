import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { AuthService } from '../../../../core/auth/auth.service';
import { ProfileService } from '../../services/profile.service';
import { Nav } from '../../../../shared/components/nav/nav';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, Nav],
  templateUrl: './profile.html',
  styleUrls: ['./profile.css']
})
export class Profile {
  private readonly auth = inject(AuthService);
  private readonly profileService = inject(ProfileService);

  feedback = signal<string | null>(null);
  user = toSignal(
    this.auth.currentUser$.pipe(
      switchMap(user => user?.userId ? this.profileService.getProfile(user.userId) : of(null)),
      startWith(null)
    ),
    { initialValue: null }
  );


  getRoleLabel(role?: string): string {
    return role === 'Instructor' ? 'Instructor'
      : role === 'Admin' ? 'Administrator'
      : 'Member';
  }
}