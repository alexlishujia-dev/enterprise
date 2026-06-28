<template>
  <view class="page">
    <view class="search-wrap">
      <u-search
        v-model="keyword"
        placeholder="搜索角色编码/名称"
        show-action
        action-text="搜索"
        bg-color="#f5f6f8"
        @search="onSearch"
        @custom="onSearch" />
    </view>

    <view class="page-container">
      <u-loading-icon v-if="state.loading && !state.items.length" text="加载中..." />

      <view v-else-if="state.items.length" class="card card--flat">
        <view
          v-for="(item, index) in state.items"
          :key="item.id"
          class="role-row"
          :class="{ 'role-row--border': index < state.items.length - 1 }"
          @click="goEdit(item.id)">
          <view class="flex-row">
            <view class="role-row__icon">
              <u-icon name="tags" color="#722ed1" size="18" />
            </view>
            <view class="flex-1">
              <view class="flex-row">
                <text class="role-row__name text-ellipsis">{{ item.roleName }}</text>
                <u-tag
                  :text="item.isActive ? '启用' : '禁用'"
                  :type="item.isActive ? 'success' : 'info'"
                  size="mini"
                  custom-style="margin-left: 12rpx; flex-shrink: 0;" />
              </view>
              <text class="role-row__code muted">{{ item.roleCode }}</text>
              <text v-if="item.description" class="role-row__desc muted">{{ item.description }}</text>
            </view>
          </view>
        </view>
        <u-loadmore :status="loadStatus" custom-style="padding: 24rpx 0;" />
      </view>

      <empty-view v-else text="暂无角色数据" />
    </view>

    <fab-button
      :visible="canCreate"
      @click="goCreate" />
  </view>
</template>

<script setup>
import { computed, reactive, ref } from 'vue';
import { onPullDownRefresh, onReachBottom, onShow } from '@dcloudio/uni-app';
import EmptyView from '@/components/empty-view/empty-view.vue';
import FabButton from '@/components/fab-button/fab-button.vue';
import env from '@/config/env';
import { createPagedState } from '@/model/page.model';
import * as roleService from '@/service/role.service';
import { useUserStore } from '@/store/user';

const userStore = useUserStore();
const keyword = ref('');
const state = reactive(createPagedState());

const loadStatus = computed(() => {
  if (state.loading) return 'loading';
  if (state.finished) return 'nomore';
  return 'loadmore';
});

const canCreate = computed(() => userStore.hasPermission('system.roles:create'));

onShow(() => {
  if (!userStore.isLoggedIn) {
    uni.reLaunch({ url: '/pages/login/login' });
    return;
  }
  if (!userStore.hasPermission('system.roles:view')) {
    uni.showToast({ title: '无权查看角色', icon: 'none' });
    return;
  }
  loadRoles(true);
});

onPullDownRefresh(async () => {
  await loadRoles(true);
  uni.stopPullDownRefresh();
});

onReachBottom(() => {
  if (!state.finished && !state.loading) {
    state.pageIndex += 1;
    loadRoles(false);
  }
});

function onSearch() {
  loadRoles(true);
}

function goEdit(id) {
  if (!userStore.hasPermission('system.roles:edit')) {
    uni.showToast({ title: '无权编辑角色', icon: 'none' });
    return;
  }
  uni.navigateTo({ url: `/pages/system/role-form/role-form?id=${id}` });
}

function goCreate() {
  uni.navigateTo({ url: '/pages/system/role-form/role-form' });
}

async function loadRoles(reset) {
  if (reset) {
    state.pageIndex = 1;
    state.finished = false;
    state.items = [];
  }

  state.loading = true;
  try {
    const data = await roleService.loadRoles({
      pageIndex: state.pageIndex,
      pageSize: env.defaultPageSize,
      keyword: keyword.value.trim()
    });
    const items = data?.items || [];
    state.total = data?.total || 0;
    state.items = reset ? items : [...state.items, ...items];
    state.finished = state.items.length >= state.total;
  } catch (err) {
    uni.showToast({ title: err.message || '加载失败', icon: 'none' });
  } finally {
    state.loading = false;
  }
}
</script>

<style scoped lang="scss">
.search-wrap {
  padding: 20rpx $page-padding;
  background: #fff;
  border-bottom: 1rpx solid $uni-border-color;
}

.role-row {
  padding: 28rpx 0;

  &--border {
    border-bottom: 1rpx solid $uni-border-color;
  }

  &__icon {
    width: 64rpx;
    height: 64rpx;
    margin-right: 20rpx;
    border-radius: 16rpx;
    background: #f9f0ff;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  &__name {
    font-size: 30rpx;
    font-weight: 600;
    color: $uni-text-color;
    max-width: 360rpx;
  }

  &__code {
    display: block;
    margin-top: 8rpx;
    font-size: 24rpx;
  }

  &__desc {
    display: block;
    margin-top: 8rpx;
    font-size: 24rpx;
    line-height: 1.5;
  }
}

.role-row:active {
  opacity: 0.85;
}
</style>
