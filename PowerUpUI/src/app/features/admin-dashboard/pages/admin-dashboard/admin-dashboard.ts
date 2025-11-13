import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith } from 'rxjs';

import { AdminDashboardService } from '../../services/admin-dashboard.service';
import { Nav } from '../../../../shared/components/nav/nav';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, Nav],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css']
})
export class AdminDashboard {
  private readonly service = inject(AdminDashboardService);

  summary = toSignal(
    this.service.getOverview().pipe(startWith(null)),
    { initialValue: null }
  );

  
  cards = [
    { label: 'Total Users', value: () => this.summary()?.totalUsers ?? 0 },
    { label: 'Members', value: () => this.summary()?.totalMembers ?? 0 },
    { label: 'Instructors', value: () => this.summary()?.totalInstructors ?? 0 },
    { label: 'New Users (30 days)', value: () => this.summary()?.newUsersLast30Days ?? 0 },
    { label: 'Active Subscriptions', value: () => this.summary()?.activeSubscriptions ?? 0 },
    {
      label: 'Monthly Revenue',
      value: () => `$${(this.summary()?.monthlyRevenue ?? 0).toFixed(2)}`
    },
    {
      label: 'Top Membership',
      value: () => this.summary()?.topMembership ?? 'N/A',
      secondary: () => `${this.summary()?.topMembershipSubscriptions ?? 0} subscriptions`
    },
    {
      label: 'Upcoming Group Classes',
      value: () => this.summary()?.upcomingGroupClasses ?? 0
    },
    {
      label: 'PT Sessions (7 days)',
      value: () => this.summary()?.personalTrainingSessionsNext7Days ?? 0
    }
  ];
}